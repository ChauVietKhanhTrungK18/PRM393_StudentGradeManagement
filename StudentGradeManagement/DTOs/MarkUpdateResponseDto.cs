namespace StudentGradeManagement.DTOs
{
    public class MarkUpdateResponseDto
    {
        public bool Success { get; set; }

        public bool IsValid { get; set; }

        public string Message { get; set; } = string.Empty;

        public decimal Value { get; set; }
    }
}
