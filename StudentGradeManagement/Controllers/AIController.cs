using BusinessLayer.DTOs;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Mvc;

namespace StudentGradeManagement.Controllers
{
    /// <summary>AI-powered grade analysis endpoints</summary>
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _ai;

        public AIController(IAIService ai) => _ai = ai;

        /// <summary>Hỏi đáp tự do về lớp học bằng tiếng Việt</summary>
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AIChatRequestDto request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.SubjectCode) ||
                string.IsNullOrWhiteSpace(request.ClassName) ||
                string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("subjectCode, className và question không được để trống.");

            var result = await _ai.ChatAsync(request, ct);
            return Ok(result);
        }

        /// <summary>Thống kê điểm + nhận xét AI tự động</summary>
        [HttpGet("statistics/{subjectCode}/{className}")]
        public async Task<IActionResult> GetStatistics(string subjectCode, string className, CancellationToken ct)
        {
            var result = await _ai.GetStatisticsAsync(subjectCode, className, ct);
            return result == null ? NotFound($"Không tìm thấy lớp {className} môn {subjectCode}.") : Ok(result);
        }

        /// <summary>Phát hiện điểm bất thường (z-score, input error, inconsistent pattern)</summary>
        [HttpGet("anomalies/{subjectCode}/{className}")]
        public async Task<IActionResult> GetAnomalies(string subjectCode, string className, CancellationToken ct)
        {
            var result = await _ai.GetAnomaliesAsync(subjectCode, className, ct);
            return result == null ? NotFound($"Không tìm thấy lớp {className} môn {subjectCode}.") : Ok(result);
        }

        /// <summary>Gợi ý comment cho từng sinh viên bằng AI</summary>
        [HttpPost("suggest-comments/{subjectCode}/{className}")]
        public async Task<IActionResult> SuggestComments(string subjectCode, string className, CancellationToken ct)
        {
            var result = await _ai.SuggestCommentsAsync(subjectCode, className, ct);
            return result == null ? NotFound($"Không tìm thấy lớp {className} môn {subjectCode}.") : Ok(result);
        }
    }
}
