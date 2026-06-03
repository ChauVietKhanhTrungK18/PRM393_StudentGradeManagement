using System.Collections.Generic;

namespace BusinessLayer.DTOs
{
    public class MarkBulkUpdateResultDto
    {
        public bool Success { get; set; }

        public bool IsValid { get; set; }

        public string Message { get; set; } = string.Empty;

        public Dictionary<string, MarkCellResultDto> Results { get; set; } = new();
    }

    public class MarkCellResultDto
    {
        public bool IsValid { get; set; }

        public string Message { get; set; } = string.Empty;

        public decimal Value { get; set; }
    }
}
