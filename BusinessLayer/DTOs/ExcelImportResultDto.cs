namespace BusinessLayer.DTOs
{
    public class ExcelImportResultDto
    {
        public int SubjectClassCount { get; set; }

        public int StudentCount { get; set; }

        public int ComponentCount { get; set; }

        public int MarkCount { get; set; }

        public int SkippedRows { get; set; }

        public List<ExcelImportLogEntryDto> ImportLog { get; set; } = new();
    }

    public class ExcelImportLogEntryDto
    {
        public string Level { get; set; } = "Info";

        public string Message { get; set; } = string.Empty;

        public DateTimeOffset Timestamp { get; set; }
    }
}
