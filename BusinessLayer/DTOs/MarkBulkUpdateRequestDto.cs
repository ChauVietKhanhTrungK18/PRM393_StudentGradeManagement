using System.Collections.Generic;

namespace BusinessLayer.DTOs
{
    public class MarkBulkUpdateRequestDto
    {
        public string SubjectCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string RollNumber { get; set; } = string.Empty;

        public Dictionary<string, string?> Marks { get; set; } = new();
    }
}
