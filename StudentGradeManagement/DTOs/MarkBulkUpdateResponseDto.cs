using System.Collections.Generic;

namespace StudentGradeManagement.DTOs
{
    public class MarkBulkUpdateResponseDto
    {
        public bool Success { get; set; }

        public bool IsValid { get; set; }

        public string Message { get; set; } = string.Empty;

        public Dictionary<string, MarkCellResponseDto> Results { get; set; } = new();
    }

    public class MarkCellResponseDto
    {
        public bool IsValid { get; set; }

        public string Message { get; set; } = string.Empty;

        public decimal Value { get; set; }
    }
}
