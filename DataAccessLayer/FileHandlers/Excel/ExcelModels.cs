namespace DataAccessLayer.FileHandlers.Excel
{
    public class ExcelWorkbookInfo
    {
        public List<ExcelSheetInfo> Sheets { get; set; } = new();
    }

    public class ExcelSheetInfo
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Headers { get; set; } = new();
    }

    public class ExcelRowData
    {
        public int RowNumber { get; set; }

        public Dictionary<string, string> Values { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
