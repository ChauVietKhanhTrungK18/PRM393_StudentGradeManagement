namespace BusinessLayer.DTOs
{
    public class ExcelSyncRequestDto
    {
        public string FilePath { get; set; } = string.Empty;
    }

    public class ExcelSyncResultDto
    {
        public string FilePath { get; set; } = string.Empty;

        public bool Success { get; set; }

        public int TotalSheets { get; set; }

        public int ImportedSheets { get; set; }

        public int SkippedSheets { get; set; }

        public List<ExcelSheetSyncResultDto> Sheets { get; set; } = new();
    }

    public class ExcelSheetSyncResultDto
    {
        public string SheetName { get; set; } = string.Empty;

        public string? SubjectCode { get; set; }

        public string? ClassName { get; set; }

        /// <summary>imported | no_changes | skipped</summary>
        public string Status { get; set; } = string.Empty;

        public string? Message { get; set; }

        public int UpdatedStudents { get; set; }

        public int UpdatedMarks { get; set; }

        public int ChangedMarksPreview { get; set; }

        public List<string> NotFoundRolls { get; set; } = new();
    }
}
