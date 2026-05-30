
using System.Collections.Generic;

namespace DataAccessLayer.Entities
{
    public class Student
    {
        public int Id { get; set; }

        public int SubjectClassId { get; set; }

        public string RollNumber { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Comment { get; set; }

        // Navigation
        public SubjectClass SubjectClass { get; set; } = null!;

        public ICollection<Mark> Marks { get; set; } = new HashSet<Mark>();

        public ICollection<AuditLog> AuditLogs { get; set; } = new HashSet<AuditLog>();
    }
}
