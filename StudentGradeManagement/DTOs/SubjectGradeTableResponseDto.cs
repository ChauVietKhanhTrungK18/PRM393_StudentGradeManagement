using System.Collections.Generic;

namespace StudentGradeManagement.DTOs
{
    public class SubjectGradeTableResponseDto
    {
        public string SubjectCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public List<string> Components { get; set; } = new();

        public List<StudentGradeRowResponseDto> Students { get; set; } = new();
    }

    public class StudentGradeRowResponseDto
    {
        public string RollNumber { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Comment { get; set; }

        public Dictionary<string, decimal?> Marks { get; set; } = new();
    }
}
