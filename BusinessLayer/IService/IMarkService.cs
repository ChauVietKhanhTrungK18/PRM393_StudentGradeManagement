using BusinessLayer.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLayer.IService
{
    public interface IMarkService
    {
        Task<MarkUpdateResultDto> UpdateMarkAsync(
            MarkUpdateRequestDto request,
            CancellationToken cancellationToken = default);

        Task<MarkBulkUpdateResultDto> UpdateMarksAsync(
            MarkBulkUpdateRequestDto request,
            CancellationToken cancellationToken = default);

        Task<MarkUpdateResultDto> ClearMarkAsync(
            MarkClearByNameRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
