using System;

namespace DataAccessLayer.Entities
{
    public class Mark
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public int ComponentId { get; set; }

        public decimal Value { get; set; }

        public string? Comment { get; set; }

        // Navigation
        public Student Student { get; set; } = null!;

        public GradingComponent Component { get; set; } = null!;
    }
}
