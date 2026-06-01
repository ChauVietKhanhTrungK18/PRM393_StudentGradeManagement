using BusinessLayer.DTOs;

namespace BusinessLayer.IService
{
    public interface IExcelImportService
    {
        Task<ExcelReadResultDto> ReadWorkbookAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        Task<ExcelPreviewResultDto> PreviewAsync(
            string filePath,
            ExcelColumnMappingDto mapping,
            CancellationToken cancellationToken = default);

        Task<ExcelImportResultDto> ImportAsync(
            string filePath,
            ExcelColumnMappingDto mapping,
            string changedBy,
            CancellationToken cancellationToken = default);
    }
}
