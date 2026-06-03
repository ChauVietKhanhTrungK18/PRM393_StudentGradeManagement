#nullable enable

using BusinessLayer.DTOs;
using BusinessLayer.IService;
using DataAccessLayer.DbContexts;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BusinessLayer.Services
{
    public class AIService : IAIService
    {
        private readonly HttpClient _http;
        private readonly AppDbContext _db;
        private readonly AIOptions _options;

        public AIService(HttpClient http, AppDbContext db, AIOptions options)
        {
            _http = http;
            _db = db;
            _options = options;
        }

        // ─── Claude API ───────────────────────────────────────────────────────

        private async Task<string> CallClaudeAsync(string prompt, CancellationToken ct)
        {
            var isLmStudio = _options.BaseUrl.Contains("127.0.0.1") || _options.BaseUrl.Contains("localhost");
            if (!isLmStudio && string.IsNullOrWhiteSpace(_options.ApiKey))
                return "AI chưa được cấu hình. Vui lòng thêm API key vào appsettings.json mục AI:ApiKey.";

            var bodyJson = JsonSerializer.Serialize(new
            {
                model = _options.Model,
                max_tokens = _options.MaxTokens,
                messages = new[] { new { role = "user", content = prompt } }
            });

            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/v1/messages";
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", string.IsNullOrWhiteSpace(_options.ApiKey) ? "lm-studio" : _options.ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            HttpResponseMessage response;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                response = await _http.SendAsync(request, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return "Yêu cầu AI hết thời gian. Vui lòng thử lại.";
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return $"Lỗi Claude API ({(int)response.StatusCode}): {json}";

            var node = JsonNode.Parse(json);
            return node?["content"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
        }

        // ─── Data Loading ─────────────────────────────────────────────────────

        private async Task<SubjectClass?> LoadAsync(string subjectCode, string className, CancellationToken ct)
        {
            return await _db.SubjectClasses
                .Include(sc => sc.GradingComponents)
                .Include(sc => sc.Students)
                    .ThenInclude(s => s.Marks)
                .AsNoTracking()
                .FirstOrDefaultAsync(sc => sc.SubjectCode == subjectCode && sc.ClassName == className, ct);
        }

        // ─── Grade Logic ──────────────────────────────────────────────────────

        private static decimal? ComputeTotal(Student student, IEnumerable<GradingComponent> components)
        {
            var totalWeight = 0m;
            var weightedSum = 0m;
            foreach (var comp in components)
            {
                var mark = student.Marks.FirstOrDefault(m => m.ComponentId == comp.Id);
                if (mark == null) return null;
                totalWeight += comp.Weight;
                weightedSum += mark.Value * comp.Weight;
            }
            return totalWeight == 0 ? null : weightedSum / totalWeight;
        }

        private static bool IsPass(decimal total, Student student, IEnumerable<GradingComponent> components)
        {
            if (total < 5.0m) return false;
            foreach (var comp in components.Where(c => c.IsCondition))
            {
                var mark = student.Marks.FirstOrDefault(m => m.ComponentId == comp.Id);
                if (mark != null && mark.Value == 0) return false;
            }
            return true;
        }

        private static string GetGradeLetter(decimal score) => score switch
        {
            >= 8.5m => "A",
            >= 8.0m => "B+",
            >= 7.0m => "B",
            >= 6.5m => "C+",
            >= 5.5m => "C",
            >= 5.0m => "D+",
            >= 4.0m => "D",
            _ => "F"
        };

        // ─── Prompt Builder ───────────────────────────────────────────────────

        private static string BuildClassSummary(SubjectClass sc)
        {
            var allComps = sc.GradingComponents.OrderBy(c => c.Name).ToList();
            var students = sc.Students.ToList();

            // Thành phần có ít nhất 1 điểm != 0 (thực sự đã nhập)
            var meaningfulComps = allComps
                .Where(c => students.Any(s =>
                {
                    var m = s.Marks.FirstOrDefault(x => x.ComponentId == c.Id);
                    return m != null && m.Value > 0m;
                }))
                .ToList();

            // Thành phần toàn 0 hoặc chưa nhập
            var zeroComps = allComps.Except(meaningfulComps).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Lớp: {sc.ClassName} | Môn: {sc.SubjectCode} | Tổng: {students.Count} sinh viên");
            if (zeroComps.Any())
                sb.AppendLine($"Chưa thi / chưa nhập điểm: {string.Join(", ", zeroComps.Select(c => c.Name))}");

            // ── PHẦN 1: Thống kê + xếp hạng từng thành phần ──────────────────────
            if (meaningfulComps.Any())
            {
                sb.AppendLine("\n=== THỐNG KÊ TỪNG THÀNH PHẦN (số liệu chính xác — đọc trực tiếp) ===");

                foreach (var comp in meaningfulComps)
                {
                    // Lấy điểm kèm thông tin sinh viên, sắp xếp từ cao xuống thấp
                    var ranked = students
                        .Select(s => new
                        {
                            s.RollNumber,
                            s.FullName,
                            Score = s.Marks.FirstOrDefault(m => m.ComponentId == comp.Id)?.Value
                        })
                        .Where(x => x.Score.HasValue && x.Score.Value > 0m)
                        .OrderByDescending(x => x.Score)
                        .ToList();

                    if (!ranked.Any()) continue;

                    var vals = ranked.Select(x => x.Score!.Value).ToList();

                    sb.AppendLine($"\n[{comp.Name}] — {vals.Count} sinh viên");
                    sb.AppendLine($"  Trung bình: {vals.Average():F2}");
                    sb.AppendLine($"  Cao nhất: {vals.Max():F1} — {ranked.First().RollNumber} {ranked.First().FullName}");
                    sb.AppendLine($"  Thấp nhất: {vals.Min():F1} — {ranked.Last().RollNumber} {ranked.Last().FullName}");
                    sb.AppendLine($"  Điểm lớn hơn 8: {vals.Count(v => v > 8m)} người");
                    sb.AppendLine($"  Điểm từ 8 trở lên (>=8): {vals.Count(v => v >= 8m)} người");
                    sb.AppendLine($"  Điểm từ 7 trở lên (>=7): {vals.Count(v => v >= 7m)} người");
                    sb.AppendLine($"  Điểm nhỏ hơn 7: {vals.Count(v => v < 7m)} người");
                    sb.AppendLine($"  Điểm nhỏ hơn 5: {vals.Count(v => v < 5m)} người");

                    // Top 5 điểm cao nhất
                    sb.AppendLine($"  Top {Math.Min(5, ranked.Count)} điểm cao nhất:");
                    foreach (var r in ranked.Take(5))
                        sb.AppendLine($"    {r.RollNumber} {r.FullName}: {r.Score:F1}");

                    // Bottom 3 điểm thấp nhất (nếu có đủ)
                    if (ranked.Count > 5)
                    {
                        sb.AppendLine($"  3 điểm thấp nhất:");
                        foreach (var r in ranked.TakeLast(3))
                            sb.AppendLine($"    {r.RollNumber} {r.FullName}: {r.Score:F1}");
                    }
                }
                sb.AppendLine("\n=== KẾT THÚC THỐNG KÊ ===");
            }

            // ── PHẦN 2: Bảng tra cứu điểm từng sinh viên (có label rõ ràng) ──────
            int passCount = 0, failCount = 0;
            var totals = new List<decimal>();

            sb.AppendLine("\n=== BẢNG ĐIỂM SINH VIÊN (tra cứu khi hỏi về cá nhân) ===");
            foreach (var s in students.OrderBy(x => x.RollNumber))
            {
                var total = ComputeTotal(s, allComps);
                bool? pass = total.HasValue ? IsPass(total.Value, s, allComps) : null;
                if (total.HasValue) { if (pass == true) passCount++; else failCount++; totals.Add(total.Value); }

                // Format: MSSV Tên | TênThành Phần=điểm | ...
                var scoreStr = string.Join(" | ", meaningfulComps.Select(c =>
                {
                    var m = s.Marks.FirstOrDefault(x => x.ComponentId == c.Id);
                    return m == null ? $"{c.Name}=?" : $"{c.Name}={m.Value:F1}";
                }));

                var totalStr = total.HasValue ? $" | Tổng={total.Value:F1} ({(pass == true ? "Đậu" : "Rớt")})" : "";
                sb.AppendLine($"{s.RollNumber} {s.FullName} | {scoreStr}{totalStr}");
            }

            if (totals.Count > 0)
                sb.AppendLine($"\nTóm tắt: {passCount} đậu, {failCount} rớt. Điểm TB tổng kết: {totals.Average():F2}");
            else if (meaningfulComps.Any())
                sb.AppendLine($"\nChưa tính được tổng kết (thiếu: {string.Join(", ", zeroComps.Select(c => c.Name))})");

            return sb.ToString();
        }

        // ─── Feature 1: Chat ──────────────────────────────────────────────────

        public async Task<AIChatResponseDto> ChatAsync(AIChatRequestDto request, CancellationToken ct = default)
        {
            var sc = await LoadAsync(request.SubjectCode, request.ClassName, ct);
            if (sc == null)
                return new AIChatResponseDto
                {
                    Answer = $"Không tìm thấy lớp {request.ClassName} môn {request.SubjectCode}."
                };

            var comps = sc.GradingComponents.ToList();
            var studentTotals = sc.Students
                .Select(s => new { s, total = ComputeTotal(s, comps) })
                .Where(x => x.total.HasValue)
                .ToList();

            var passCount = studentTotals.Count(x => IsPass(x.total!.Value, x.s, comps));
            var failCount = studentTotals.Count - passCount;
            var avgScore = studentTotals.Count > 0 ? studentTotals.Average(x => (double)x.total!.Value) : 0;

            var prompt = $"""
                Bạn là trợ lý AI cho hệ thống quản lý điểm sinh viên. Trả lời bằng tiếng Việt, ngắn gọn, chính xác.

                QUAN TRỌNG:
                - Phần [THỐNG KÊ TỪNG THÀNH PHẦN] đã được tính sẵn bằng máy tính — hãy dùng trực tiếp các con số đó, KHÔNG tự đếm lại từ danh sách sinh viên.
                - Nếu câu hỏi hỏi về số lượng sinh viên có điểm X > ngưỡng, hãy đọc dòng ">X: NSV" trong phần thống kê.
                - Nếu câu hỏi hỏi về điểm trung bình, min, max — đọc từ phần thống kê.
                - Chỉ dùng danh sách sinh viên khi câu hỏi hỏi về sinh viên cụ thể (tên, MSSV).

                Dữ liệu lớp học:
                {BuildClassSummary(sc)}

                Câu hỏi: {request.Question}

                Trả lời:
                """;

            var answer = await CallClaudeAsync(prompt, ct);

            return new AIChatResponseDto
            {
                Answer = answer,
                RelatedData = new AIChatRelatedDataDto
                {
                    TotalStudents = sc.Students.Count,
                    PassCount = passCount,
                    FailCount = failCount,
                    AverageScore = Math.Round(avgScore, 2)
                }
            };
        }

        // ─── Feature 2: Statistics ────────────────────────────────────────────

        public async Task<AIStatisticsResponseDto?> GetStatisticsAsync(string subjectCode, string className, CancellationToken ct = default)
        {
            var sc = await LoadAsync(subjectCode, className, ct);
            if (sc == null) return null;

            var comps = sc.GradingComponents.OrderBy(c => c.Name).ToList();
            var students = sc.Students.ToList();

            var studentTotals = students
                .Select(s => new { Student = s, Total = ComputeTotal(s, comps) })
                .ToList();

            var complete = studentTotals.Where(x => x.Total.HasValue).ToList();
            int passCount = complete.Count(x => IsPass(x.Total!.Value, x.Student, comps));
            int failCount = complete.Count - passCount;
            double avgScore = complete.Count > 0 ? (double)complete.Average(x => x.Total!.Value) : 0;

            // Grade distribution
            var gradeDistrib = new Dictionary<string, int>
            {
                ["A"] = 0, ["B+"] = 0, ["B"] = 0, ["C+"] = 0,
                ["C"] = 0, ["D+"] = 0, ["D"] = 0, ["F"] = 0
            };
            foreach (var x in complete)
            {
                var letter = IsPass(x.Total!.Value, x.Student, comps)
                    ? GetGradeLetter(x.Total.Value)
                    : "F";
                gradeDistrib[letter]++;
            }

            // Component stats (pure C#)
            var compStats = comps.Select(c =>
            {
                var marks = students
                    .Select(s => s.Marks.FirstOrDefault(m => m.ComponentId == c.Id))
                    .ToList();
                var values = marks.Where(m => m != null).Select(m => m!.Value).ToList();
                return new ComponentStatDto
                {
                    Name = c.Name,
                    Average = values.Count > 0 ? Math.Round((double)values.Average(), 2) : 0,
                    ZeroCount = values.Count(v => v == 0),
                    EmptyCount = marks.Count(m => m == null)
                };
            }).ToList();

            // AI insights via Claude
            var insightPrompt = $"""
                Bạn là trợ lý AI phân tích điểm thi. Dựa vào dữ liệu lớp học dưới đây, hãy sinh ra 3-5 nhận xét ngắn gọn, thực tiễn bằng tiếng Việt.
                Mỗi nhận xét trên một dòng, bắt đầu bằng dấu "-". Không thêm tiêu đề hay giải thích.

                Dữ liệu:
                {BuildClassSummary(sc)}

                Thống kê: {passCount} đậu ({(students.Count > 0 ? passCount * 100 / students.Count : 0)}%), {failCount} rớt, Điểm TB: {avgScore:F2}

                Nhận xét:
                """;

            var rawInsights = await CallClaudeAsync(insightPrompt, ct);
            var insights = rawInsights
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.TrimStart('-', ' ').Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Take(5)
                .ToList();

            return new AIStatisticsResponseDto
            {
                SubjectCode = sc.SubjectCode,
                ClassName = sc.ClassName,
                TotalStudents = students.Count,
                PassCount = passCount,
                FailCount = failCount,
                PassRate = students.Count > 0 ? Math.Round((double)passCount / students.Count, 4) : 0,
                AverageScore = Math.Round(avgScore, 2),
                ComponentStats = compStats,
                GradeDistribution = gradeDistrib,
                AiInsights = insights
            };
        }

        // ─── Feature 3: Anomaly Detection (pure C#) ──────────────────────────

        public async Task<AIAnomaliesResponseDto?> GetAnomaliesAsync(string subjectCode, string className, CancellationToken ct = default)
        {
            var sc = await LoadAsync(subjectCode, className, ct);
            if (sc == null) return null;

            var comps = sc.GradingComponents.ToList();
            var students = sc.Students.ToList();
            var anomalies = new List<AIAnomalyDto>();

            // Chỉ dùng thành phần đã có điểm thực (> 0) cho phân tích
            var activeComps = comps
                .Where(c => students.Any(s =>
                {
                    var m = s.Marks.FirstOrDefault(x => x.ComponentId == c.Id);
                    return m != null && m.Value > 0m;
                }))
                .ToList();

            // Per-component class statistics for z-score (chỉ tính trên activeComps, bỏ giá trị 0)
            var compStats = activeComps.ToDictionary(c => c.Id, c =>
            {
                var vals = students
                    .Select(s => s.Marks.FirstOrDefault(m => m.ComponentId == c.Id)?.Value)
                    .Where(v => v.HasValue && v.Value > 0m)
                    .Select(v => (double)v!.Value)
                    .ToList();
                if (vals.Count < 2) return (mean: 0.0, std: 0.0);
                var mean = vals.Average();
                var std = Math.Sqrt(vals.Select(v => (v - mean) * (v - mean)).Average());
                return (mean, std);
            });

            foreach (var student in students)
            {
                var total = ComputeTotal(student, comps);

                // POSSIBLE_INPUT_ERROR: chỉ kiểm tra khi MaxMark > 0 và điểm âm
                foreach (var comp in activeComps)
                {
                    var mark = student.Marks.FirstOrDefault(m => m.ComponentId == comp.Id);
                    if (mark == null) continue;
                    if (mark.Value < 0)
                    {
                        anomalies.Add(new AIAnomalyDto
                        {
                            RollNumber = student.RollNumber,
                            FullName = student.FullName,
                            Type = "POSSIBLE_INPUT_ERROR",
                            Description = $"{comp.Name} = {mark.Value} — điểm âm không hợp lệ.",
                            Severity = "Error"
                        });
                    }
                    else if (comp.MaxMark > 0 && mark.Value > comp.MaxMark)
                    {
                        anomalies.Add(new AIAnomalyDto
                        {
                            RollNumber = student.RollNumber,
                            FullName = student.FullName,
                            Type = "POSSIBLE_INPUT_ERROR",
                            Description = $"{comp.Name} = {mark.Value} vượt quá điểm tối đa {comp.MaxMark}.",
                            Severity = "Error"
                        });
                    }
                }

                // HIGH_COMPONENT_LOW_TOTAL
                if (total.HasValue && total.Value < 5.0m)
                {
                    var highComp = comps.FirstOrDefault(c =>
                    {
                        var m = student.Marks.FirstOrDefault(x => x.ComponentId == c.Id);
                        return m != null && m.Value >= 7.0m;
                    });
                    if (highComp != null)
                    {
                        var highMark = student.Marks.First(m => m.ComponentId == highComp.Id);
                        anomalies.Add(new AIAnomalyDto
                        {
                            RollNumber = student.RollNumber,
                            FullName = student.FullName,
                            Type = "HIGH_COMPONENT_LOW_TOTAL",
                            Description = $"{highComp.Name} = {highMark.Value:F1} nhưng tổng kết chỉ {total.Value:F1} — kiểm tra lại các thành phần khác.",
                            Severity = "Warning"
                        });
                    }
                }

                // INCONSISTENT_PATTERN: chỉ xét thành phần đã có điểm thực (activeComps, bỏ thành phần toàn 0)
                var availableMarks = activeComps
                    .Select(c => new { comp = c, mark = student.Marks.FirstOrDefault(m => m.ComponentId == c.Id) })
                    .Where(x => x.mark != null && x.mark.Value > 0m)
                    .ToList();

                if (availableMarks.Count >= 3)
                {
                    var vals = availableMarks.Select(x => (double)x.mark!.Value).ToList();
                    var mean = vals.Average();
                    var std = Math.Sqrt(vals.Select(v => (v - mean) * (v - mean)).Average());
                    if (std > 2.5)
                    {
                        var markDesc = string.Join(", ", availableMarks.Select(x => $"{x.comp.Name}={x.mark!.Value:F1}"));
                        anomalies.Add(new AIAnomalyDto
                        {
                            RollNumber = student.RollNumber,
                            FullName = student.FullName,
                            Type = "INCONSISTENT_PATTERN",
                            Description = $"Điểm không đồng đều (σ={std:F1}): {markDesc}.",
                            Severity = "Warning"
                        });
                    }
                }

                // OUTLIER_HIGH / OUTLIER_LOW per component (z-score, chỉ activeComps)
                foreach (var comp in activeComps)
                {
                    var mark = student.Marks.FirstOrDefault(m => m.ComponentId == comp.Id);
                    if (mark == null || mark.Value == 0m) continue;
                    var stat = compStats[comp.Id];
                    if (stat.std < 0.01) continue;
                    var z = ((double)mark.Value - stat.mean) / stat.std;
                    if (z > 2.0)
                    {
                        anomalies.Add(new AIAnomalyDto
                        {
                            RollNumber = student.RollNumber,
                            FullName = student.FullName,
                            Type = "OUTLIER_HIGH",
                            Description = $"{comp.Name} = {mark.Value:F1} vượt xa trung bình lớp {stat.mean:F1} (z={z:F1}).",
                            Severity = "Info"
                        });
                    }
                    else if (z < -2.0)
                    {
                        anomalies.Add(new AIAnomalyDto
                        {
                            RollNumber = student.RollNumber,
                            FullName = student.FullName,
                            Type = "OUTLIER_LOW",
                            Description = $"{comp.Name} = {mark.Value:F1} thấp hơn nhiều trung bình lớp {stat.mean:F1} (z={z:F1}).",
                            Severity = "Warning"
                        });
                    }
                }
            }

            // Suppress compiler warning - method is async to match interface
            await Task.CompletedTask;

            return new AIAnomaliesResponseDto
            {
                Anomalies = anomalies,
                Summary = anomalies.Count == 0
                    ? "Không phát hiện trường hợp bất thường."
                    : $"Phát hiện {anomalies.Count} trường hợp bất thường cần xem lại."
            };
        }



        // ─── Feature 5: Comment Suggestions ──────────────────────────────────

        public async Task<AISuggestCommentsResponseDto?> SuggestCommentsAsync(string subjectCode, string className, CancellationToken ct = default)
        {
            var sc = await LoadAsync(subjectCode, className, ct);
            if (sc == null) return null;

            var comps = sc.GradingComponents.ToList();
            var students = sc.Students.OrderBy(s => s.RollNumber).ToList();

            if (!students.Any())
                return new AISuggestCommentsResponseDto();

            var studentLines = students.Select(s =>
            {
                var total = ComputeTotal(s, comps);
                var markStr = string.Join(" ", comps.Select(c =>
                {
                    var m = s.Marks.FirstOrDefault(x => x.ComponentId == c.Id);
                    return m == null ? $"{c.Name}:N/A" : $"{c.Name}:{m.Value:F1}";
                }));
                return $"[{s.RollNumber}] {markStr} Tổng:{(total.HasValue ? total.Value.ToString("F1") : "N/A")}";
            });

            var prompt = $"""
                Bạn là trợ lý AI hỗ trợ giáo viên viết nhận xét cho sinh viên.
                Với từng sinh viên dưới đây, hãy gợi ý một câu nhận xét ngắn (tối đa 15 từ) bằng tiếng Việt, phù hợp với kết quả học tập.

                Trả lời theo định dạng: [MSSV] nhận xét
                Ví dụ: [SE123456] Kết quả tốt, tiếp tục duy trì phong độ.

                Mỗi sinh viên trên một dòng. KHÔNG thêm giải thích hay tiêu đề.

                Dữ liệu lớp {sc.ClassName} môn {sc.SubjectCode}:
                {string.Join("\n", studentLines)}

                Nhận xét:
                """;

            var raw = await CallClaudeAsync(prompt, ct);

            var suggestions = new List<AISuggestCommentDto>();
            foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var closeBracket = line.IndexOf(']');
                if (closeBracket < 2) continue;
                var roll = line[1..closeBracket].Trim();
                var comment = line[(closeBracket + 1)..].Trim();
                var student = students.FirstOrDefault(s => s.RollNumber == roll);
                if (student == null || string.IsNullOrWhiteSpace(comment)) continue;

                var total = ComputeTotal(student, comps);
                var confidence = total.HasValue
                    ? (total.Value >= 7.0m ? "HIGH" : total.Value >= 5.0m ? "MEDIUM" : "LOW")
                    : "LOW";

                suggestions.Add(new AISuggestCommentDto
                {
                    RollNumber = roll,
                    FullName = student.FullName,
                    SuggestedComment = comment,
                    Confidence = confidence
                });
            }

            // Fill in students Claude missed
            foreach (var s in students.Where(s => !suggestions.Any(x => x.RollNumber == s.RollNumber)))
            {
                suggestions.Add(new AISuggestCommentDto
                {
                    RollNumber = s.RollNumber,
                    FullName = s.FullName,
                    SuggestedComment = "Cần xem xét thêm.",
                    Confidence = "LOW"
                });
            }

            return new AISuggestCommentsResponseDto { Suggestions = suggestions };
        }

    }
}
