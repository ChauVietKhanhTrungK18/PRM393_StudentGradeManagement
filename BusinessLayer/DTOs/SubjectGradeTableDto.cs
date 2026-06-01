using System.Collections.Generic;

namespace BusinessLayer.DTOs
{
    public class SubjectGradeTableDto
    {
        public string SubjectCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public List<string> Components { get; set; } = new();

        public List<StudentGradeRowDto> Students { get; set; } = new();
    }

    public class StudentGradeRowDto
    {
        public string RollNumber { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Comment { get; set; }

        /// <summary>
        /// Key = component name, Value = null nếu chưa có điểm.
        /// </summary>
        public Dictionary<string, decimal?> Marks { get; set; } = new();
    }
}
