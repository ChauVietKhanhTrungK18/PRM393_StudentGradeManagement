namespace BusinessLayer.DTOs
{
    public class ExcelPreviewRequestDto
    {
        public string FilePath { get; set; } = string.Empty;

        public string SheetName { get; set; } = string.Empty;

        public string SubjectCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public ExcelColumnMappingDto ColumnMapping { get; set; } = new();
    }

    public class ExcelImportRequestDto
    {
        public string FilePath { get; set; } = string.Empty;

        public string SheetName { get; set; } = string.Empty;

        public string SubjectCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public ExcelColumnMappingDto ColumnMapping { get; set; } = new();

        public bool Overwrite { get; set; } = true;
    }
}
