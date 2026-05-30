namespace StudentGradeManagement.DTOs
{
    public class FGImportResponseDto
    {
        public bool Success { get; set; }

        public int SubjectClassCount { get; set; }

        public int StudentCount { get; set; }

        public int ComponentCount { get; set; }

        public int MarkCount { get; set; }

        /// <summary>
        /// Optional user-friendly message (errors, warnings).
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
