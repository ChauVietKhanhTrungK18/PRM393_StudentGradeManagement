namespace BusinessLayer.DTOs
{
    public class ExcelPreviewResultDto
    {
        public List<ExcelStudentPreviewDto> Preview { get; set; } = new();

        public List<string> NotFoundRolls { get; set; } = new();

        public int TotalRows { get; set; }

        public int ValidRows { get; set; }
    }

    public class ExcelStudentPreviewDto
    {
        public string RollNumber { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public List<ExcelMarkPreviewDto> Marks { get; set; } = new();
    }

    public class ExcelMarkPreviewDto
    {
        public string ComponentName { get; set; } = string.Empty;

        public decimal? CurrentValue { get; set; }

        public decimal? NewValue { get; set; }
    }
}
