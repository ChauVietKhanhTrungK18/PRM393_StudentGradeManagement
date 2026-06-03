namespace BusinessLayer.DTOs
{
    public class MarkUpdateRequestDto
    {
        public string SubjectCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string RollNumber { get; set; } = string.Empty;

        public string ComponentName { get; set; } = string.Empty;

        public string? RawValue { get; set; }
    }
}
