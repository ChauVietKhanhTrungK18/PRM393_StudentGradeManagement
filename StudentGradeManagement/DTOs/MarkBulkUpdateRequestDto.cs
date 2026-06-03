using System.Collections.Generic;
using System.Text.Json;

namespace StudentGradeManagement.DTOs
{
    public class MarkBulkUpdateRequestDto
    {
        public string? Comment { get; set; }

        public Dictionary<string, JsonElement> Marks { get; set; } = new();
    }
}
