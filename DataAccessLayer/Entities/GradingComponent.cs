using System.Collections.Generic;

namespace DataAccessLayer.Entities
{
    public class GradingComponent
    {
        public int Id { get; set; }

        public int SubjectClassId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal MaxMark { get; set; }

        public decimal Weight { get; set; }

        public bool IsCondition { get; set; }

        // Navigation
        public SubjectClass SubjectClass { get; set; } = null!;

        public ICollection<Mark> Marks { get; set; } = new HashSet<Mark>();

        public ICollection<AuditLog> AuditLogs { get; set; } = new HashSet<AuditLog>();
    }
}
