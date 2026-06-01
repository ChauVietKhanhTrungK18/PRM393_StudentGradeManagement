using BusinessLayer.DTOs;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.IService
{
    public interface IExcelImportService
    {
        Task<ExcelUploadResultDto> UploadAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);

        Task<ExcelPreviewResultDto> PreviewAsync(
            ExcelPreviewRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ExcelImportResultDto> ImportAsync(
            ExcelImportRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
