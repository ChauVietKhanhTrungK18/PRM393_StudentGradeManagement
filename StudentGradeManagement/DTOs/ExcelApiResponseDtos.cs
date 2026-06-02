using BusinessLayer.DTOs;

namespace StudentGradeManagement.DTOs
{
    public class ExcelUploadResponseDto
    {
        public string FilePath { get; set; } = string.Empty;

        public List<ExcelSheetInfoDto> Sheets { get; set; } = new();
    }

    public class ExcelPreviewResponseDto
    {
        public List<ExcelStudentPreviewDto> Preview { get; set; } = new();

        public List<string> NotFoundRolls { get; set; } = new();

        public int TotalRows { get; set; }

        public int ValidRows { get; set; }
    }

    public class ExcelImportResponseDto
    {
        public bool Success { get; set; }

        public int ImportedCount { get; set; }

        public int SkippedCount { get; set; }

        public List<string> NotFoundRolls { get; set; } = new();

        public List<string> Errors { get; set; } = new();
    }
}
