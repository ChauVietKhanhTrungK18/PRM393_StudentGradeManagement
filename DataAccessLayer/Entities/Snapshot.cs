#nullable enable
using System;

namespace DataAccessLayer.Entities
{
    public class Snapshot
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
