namespace StudentGradeManagement.DTOs
{
    public class ExcelSyncResponseDto
    {
        public string FilePath { get; set; } = string.Empty;

        public bool Success { get; set; }

        public int TotalSheets { get; set; }

        public int ImportedSheets { get; set; }

        public int SkippedSheets { get; set; }

        public List<ExcelSheetSyncResponseItemDto> Sheets { get; set; } = new();
    }

    public class ExcelSheetSyncResponseItemDto
    {
        public string SheetName { get; set; } = string.Empty;

        public string? SubjectCode { get; set; }

        public string? ClassName { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Message { get; set; }

        public int UpdatedStudents { get; set; }

        public int UpdatedMarks { get; set; }

        public int ChangedMarksPreview { get; set; }

        public List<string> NotFoundRolls { get; set; } = new();
    }
}
