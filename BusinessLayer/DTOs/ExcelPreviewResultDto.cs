namespace BusinessLayer.DTOs
{
    public class ExcelPreviewResultDto
    {
        public int TotalRows { get; set; }

        public int ValidRows { get; set; }

        public int InvalidRows { get; set; }

        public List<ExcelPreviewRowDto> Rows { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }

    public class ExcelPreviewRowDto
    {
        public int RowNumber { get; set; }

        public string SubjectCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string RollNumber { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Comment { get; set; }

        public Dictionary<string, decimal?> Marks { get; set; } = new();

        public bool IsValid { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}
