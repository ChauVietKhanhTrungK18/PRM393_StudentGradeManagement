namespace DataAccessLayer.FileHandlers.Excel
{
    public interface IExcelReader
    {
        Task<ExcelWorkbookInfo> GetWorkbookInfoAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        Task<List<ExcelRowData>> ReadSheetRowsAsync(
            string filePath,
            string sheetName,
            CancellationToken cancellationToken = default,
            int? maxRows = null);
    }
}
