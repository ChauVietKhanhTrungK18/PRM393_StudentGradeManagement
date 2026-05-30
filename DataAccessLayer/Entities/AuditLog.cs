using System;

namespace DataAccessLayer.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public int ComponentId { get; set; }

        public decimal? OldValue { get; set; }

        public decimal? NewValue { get; set; }

        public string ChangedBy { get; set; } = string.Empty;

        public DateTimeOffset ChangedAt { get; set; }

        // Navigation
        public Student Student { get; set; } = null!;

        public GradingComponent Component { get; set; } = null!;
    }
}
