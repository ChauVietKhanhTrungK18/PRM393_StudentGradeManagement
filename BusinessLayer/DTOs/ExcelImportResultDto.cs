namespace BusinessLayer.DTOs
{
    public class ExcelImportResultDto
    {
        public bool Success { get; set; }

        public int ImportedCount { get; set; }

        public int SkippedCount { get; set; }

        public List<string> NotFoundRolls { get; set; } = new();

        public List<string> Errors { get; set; } = new();
    }
}
