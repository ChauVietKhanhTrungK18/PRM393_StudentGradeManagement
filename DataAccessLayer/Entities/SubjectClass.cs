#nullable enable
using System.Collections.Generic;

namespace DataAccessLayer.Entities
{
    public class SubjectClass
    {
        public int Id { get; set; }

        public string SubjectCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public ICollection<Student> Students { get; set; } = new HashSet<Student>();

        public ICollection<GradingComponent> GradingComponents { get; set; } = new HashSet<GradingComponent>();
    }
}
