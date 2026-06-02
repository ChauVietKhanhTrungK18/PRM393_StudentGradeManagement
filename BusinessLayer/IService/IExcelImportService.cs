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

        /// <summary>
        /// Auto-sync all sheets: match sheet name to subject class, map columns, import changed marks.
        /// </summary>
        Task<ExcelSyncResultDto> SyncAllSheetsAsync(
            ExcelSyncRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload file then sync all sheets in one call.
        /// </summary>
        Task<ExcelSyncResultDto> UploadAndSyncAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);
    }
}
