# AI Features — Grade Student Management System

## Tổng quan

Hệ thống quản lý điểm sinh viên tích hợp AI để hỗ trợ giáo viên phân tích, kiểm tra và nhận xét điểm thi một cách tự động. AI được tích hợp vào bước **"Kiểm tra & phân tích"** trong luồng giáo viên, sau khi import điểm từ Excel.

Mô hình sử dụng: **Claude Haiku** (`claude-haiku-4-5-20251001`) — nhanh, chi phí thấp, phù hợp cho chat và phân tích theo yêu cầu.

---

## Tính năng 1 — AI Chat về lớp học

**Endpoint:** `POST /api/ai/chat`

Giáo viên đặt câu hỏi bằng tiếng Việt tự nhiên về lớp học. AI đọc dữ liệu thực từ database (điểm, sinh viên, thành phần) và trả lời chính xác.

### Ví dụ câu hỏi và trả lời

| Câu hỏi | Trả lời mẫu |
|---------|------------|
| Lớp này có bao nhiêu bạn đậu/rớt? | "Lớp NET1607 có 35 sinh viên, 28 đậu (80%) và 7 rớt (20%)." |
| Điểm trung bình môn này là bao nhiêu? | "Điểm trung bình tổng kết là 6.8/10." |
| Sinh viên nào có điểm thấp nhất? | "SE123456 — Nguyễn Văn A, tổng kết 3.2." |
| Thành phần nào có điểm trung bình thấp nhất? | "FE có điểm trung bình thấp nhất: 5.9/10." |
| Bao nhiêu ô điểm còn trống? | "Còn 4 ô trống: 2 ở FE, 2 ở PE." |
| Sinh viên có FE = 0 là bao nhiêu? | "3 sinh viên có FE = 0, tất cả sẽ tự động rớt." |
| Dự đoán ai có nguy cơ rớt? | "5 sinh viên có tổng điểm hiện tại dưới 4.5, nguy cơ rớt cao." |

### Request / Response

```json
// POST /api/ai/chat
{
  "subjectCode": "PRN231",
  "className": "NET1607",
  "question": "Lớp này có bao nhiêu bạn đậu rớt?"
}

// Response
{
  "answer": "Lớp NET1607 có 35 sinh viên, trong đó 28 đậu (80%) và 7 rớt (20%).",
  "relatedData": {
    "totalStudents": 35,
    "passCount": 28,
    "failCount": 7
  }
}
```

### Luồng xử lý

```
1. Load dữ liệu từ DB: SubjectClass + Students + Marks + GradingComponents
2. Tính toán thống kê bằng C# (pass/fail, TB, ô trống, ...)
3. Build prompt tiếng Việt với data summary + câu hỏi
4. Gọi Claude API → nhận câu trả lời
5. Trả về answer + relatedData
```

---

## Tính năng 2 — Thống kê tự động + Nhận xét AI

**Endpoint:** `GET /api/ai/statistics/{subjectCode}/{className}`

Sau khi giáo viên import điểm xong, hệ thống tự động tổng hợp thống kê và dùng AI để sinh ra 3–5 nhận xét ngắn gọn, có giá trị thực tiễn.

### Response mẫu

```json
{
  "subjectCode": "PRN231",
  "className": "NET1607",
  "totalStudents": 35,
  "passCount": 28,
  "failCount": 7,
  "passRate": 0.80,
  "averageScore": 6.8,
  "componentStats": [
    { "name": "PE",  "average": 7.2, "zeroCount": 2, "emptyCount": 0 },
    { "name": "FE",  "average": 5.9, "zeroCount": 3, "emptyCount": 1 }
  ],
  "gradeDistribution": {
    "A": 5, "B+": 8, "B": 10, "C+": 5, "C": 4, "D+": 2, "D": 1, "F": 7
  },
  "aiInsights": [
    "3 sinh viên có FE = 0 sẽ tự động rớt môn theo quy định điều kiện.",
    "Thành phần FE có điểm trung bình thấp nhất (5.9/10) — đây là điểm yếu chính của lớp.",
    "Tỷ lệ đậu 80% ở mức trung bình. Có 7 sinh viên cần xem xét lại kết quả."
  ]
}
```

### Phân loại logic

| Phần | Thực hiện bởi |
|------|--------------|
| Tính pass/fail, điểm TB, phân phối | Thuần C# |
| Tạo nhận xét (aiInsights) | Claude API |

---

## Tính năng 3 — Phát hiện điểm bất thường

**Endpoint:** `GET /api/ai/anomalies/{subjectCode}/{className}`

Tự động phát hiện các pattern bất thường trong bảng điểm mà giáo viên dễ bỏ sót khi kiểm tra thủ công.

### Các loại bất thường phát hiện được

| Loại | Mô tả | Ví dụ |
|------|-------|-------|
| `HIGH_COMPONENT_LOW_TOTAL` | Điểm thành phần cao nhưng tổng thấp bất thường | PE = 9.5 nhưng tổng = 3.0 |
| `OUTLIER_HIGH` | Điểm vượt xa mức trung bình lớp (> 2 độ lệch chuẩn) | FE = 10 trong khi TB lớp = 5.2 |
| `OUTLIER_LOW` | Điểm thấp bất thường so với cả lớp | PE = 1.0 trong khi TB lớp = 7.5 |
| `INCONSISTENT_PATTERN` | Điểm các thành phần không nhất quán | GK = 9, PE = 9, FE = 1 |
| `POSSIBLE_INPUT_ERROR` | Khả năng nhập sai (điểm vượt max, số âm) | FE = 11 (max = 10) |

### Response mẫu

```json
{
  "anomalies": [
    {
      "rollNumber": "SE123456",
      "fullName": "Nguyễn Văn A",
      "type": "HIGH_COMPONENT_LOW_TOTAL",
      "description": "PE = 9.5 nhưng tổng kết chỉ 3.2 — kiểm tra lại điểm FE.",
      "severity": "Warning"
    }
  ],
  "summary": "Phát hiện 3 trường hợp bất thường cần xem lại."
}
```

---

## Tính năng 4 — Dự đoán rủi ro rớt môn

**Endpoint:** `GET /api/ai/risk/{subjectCode}/{className}`

Dựa vào điểm các thành phần **đã có** (trước khi có điểm FE cuối kỳ), AI dự đoán sinh viên nào có nguy cơ rớt cao để giáo viên có thể can thiệp sớm.

### Cách tính risk score

```
risk_score = f(
  tỷ lệ thành phần đã có / tổng thành phần,
  điểm trung bình các thành phần đã có so với ngưỡng đậu,
  có thành phần điều kiện = 0 không
)
```

### Response mẫu

```json
{
  "atRiskStudents": [
    {
      "rollNumber": "SE789",
      "fullName": "Trần Thị B",
      "currentAvailable": 4.2,
      "riskLevel": "HIGH",
      "reason": "Điểm PE = 0 (thành phần điều kiện) — sẽ tự động rớt nếu không được xem xét lại."
    },
    {
      "rollNumber": "SE456",
      "fullName": "Lê Văn C",
      "currentAvailable": 4.8,
      "riskLevel": "MEDIUM",
      "reason": "Tổng điểm hiện tại 4.8, cần ít nhất 5.2 điểm FE để đậu."
    }
  ]
}
```

---

## Tính năng 5 — Gợi ý comment tự động

**Endpoint:** `POST /api/ai/suggest-comments/{subjectCode}/{className}`

AI phân tích pattern điểm của từng sinh viên và gợi ý comment phù hợp. Giáo viên chỉ cần duyệt hoặc chỉnh sửa thay vì viết tay từng người.

### Template comment theo pattern

| Pattern điểm | Comment gợi ý |
|-------------|--------------|
| Tất cả thành phần cao | "Kết quả tốt, duy trì." |
| GK cao, FE thấp | "Cần cải thiện kỹ năng làm bài thi cuối kỳ." |
| PE = 0 (điều kiện) | "Vắng thực hành — cần xem xét trường hợp đặc biệt." |
| Điểm đều thấp | "Cần hỗ trợ thêm về kiến thức nền tảng." |
| Điểm không đều giữa các thành phần | "Học không đồng đều, cần cân bằng giữa lý thuyết và thực hành." |

### Response mẫu

```json
{
  "suggestions": [
    {
      "rollNumber": "SE123",
      "fullName": "Nguyễn Văn A",
      "suggestedComment": "Kết quả tốt, duy trì.",
      "confidence": "HIGH"
    },
    {
      "rollNumber": "SE456",
      "fullName": "Trần Thị B",
      "suggestedComment": "Cần cải thiện kỹ năng làm bài thi cuối kỳ.",
      "confidence": "MEDIUM"
    }
  ]
}
```

---

## Kiến trúc tích hợp AI

```
Flutter App
    │
    ├── POST /api/ai/chat                    ← Hỏi đáp tự do
    ├── GET  /api/ai/statistics/{sub}/{cls}  ← Thống kê + nhận xét
    ├── GET  /api/ai/anomalies/{sub}/{cls}   ← Phát hiện bất thường
    ├── GET  /api/ai/risk/{sub}/{cls}        ← Dự đoán rủi ro
    └── POST /api/ai/suggest-comments/...   ← Gợi ý comment
    │
AIController (ASP.NET Core)
    │
AIService
    ├── DataLoader   — đọc dữ liệu từ SQLite qua SubjectService
    ├── StatEngine   — tính toán thuần C# (pass/fail, TB, phân phối)
    ├── PromptBuilder — ghép data summary + câu hỏi thành prompt
    └── ClaudeClient  — gọi Claude API qua HttpClient
```

### Phân chia trách nhiệm

| Phần | Thực hiện bởi | Lý do |
|------|--------------|-------|
| Tính pass/fail, điểm TB, phân phối | C# | Chính xác 100%, không tốn token |
| Phát hiện outlier (z-score) | C# | Thuật toán cố định, không cần LLM |
| Nhận xét ngôn ngữ tự nhiên | Claude API | LLM giỏi viết văn bản |
| Trả lời câu hỏi mở | Claude API | Cần hiểu ngữ nghĩa câu hỏi |

---

## Cấu hình

**appsettings.json:**
```json
"AI": {
  "ApiKey": "sk-ant-...",
  "Model": "claude-haiku-4-5-20251001",
  "MaxTokens": 1024,
  "TimeoutSeconds": 30
}
```

**Program.cs:**
```csharp
builder.Services.AddHttpClient<IAIService, AIService>();
builder.Services.Configure<AIOptions>(builder.Configuration.GetSection("AI"));
```

---

## Kế hoạch triển khai

```
Sprint 4a (1 ngày)   — Tính năng 2: Statistics + aiInsights
                        Không cần chat UI, giá trị ngay lập tức

Sprint 4b (1-2 ngày) — Tính năng 1: AI Chat
                        Core feature, cần thiết nhất cho UX

Sprint 4c (1 ngày)   — Tính năng 3: Anomaly Detection
                        Phần lớn là C# thuần, ít phụ thuộc Claude

Sprint 5a (1 ngày)   — Tính năng 4: Risk Prediction
Sprint 5b (1 ngày)   — Tính năng 5: Auto Comment Suggestion
```

---

## Files cần tạo

```
StudentGradeManagement/Controllers/
└── AIController.cs

BusinessLayer/IService/
└── IAIService.cs

BusinessLayer/Services/
└── AIService.cs

StudentGradeManagement/DTOs/
└── AIDtos.cs
```
