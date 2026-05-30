using BusinessLayer.DTOs;

namespace StudentGradeManagement.DTOs
{
    public class ExcelReadResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<ExcelSheetInfoDto> Sheets { get; set; } = new();
    }

    public class ExcelPreviewResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int TotalRows { get; set; }

        public int ValidRows { get; set; }

        public int InvalidRows { get; set; }

        public List<ExcelPreviewRowDto> Rows { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }

    public class ExcelImportResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int SubjectClassCount { get; set; }

        public int StudentCount { get; set; }

        public int ComponentCount { get; set; }

        public int MarkCount { get; set; }

        public int SkippedRows { get; set; }

        public List<ExcelImportLogEntryDto> ImportLog { get; set; } = new();
    }
}
