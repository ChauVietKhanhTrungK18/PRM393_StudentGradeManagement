#nullable enable

using BusinessLayer.DTOs;

namespace BusinessLayer.IService
{
    public interface IAIService
    {
        Task<AIChatResponseDto> ChatAsync(AIChatRequestDto request, CancellationToken ct = default);
        Task<AIStatisticsResponseDto?> GetStatisticsAsync(string subjectCode, string className, CancellationToken ct = default);
        Task<AIAnomaliesResponseDto?> GetAnomaliesAsync(string subjectCode, string className, CancellationToken ct = default);
        Task<AISuggestCommentsResponseDto?> SuggestCommentsAsync(string subjectCode, string className, CancellationToken ct = default);
    }
}
