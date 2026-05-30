using System;

namespace DataAccessLayer.Entities
{
    public class Session
    {
        public int Id { get; set; }

        public string FGPath { get; set; } = string.Empty;

        public string TeacherName { get; set; } = string.Empty;

        public string Semester { get; set; } = string.Empty;

        public DateTimeOffset OpenedAt { get; set; }
    }
}
