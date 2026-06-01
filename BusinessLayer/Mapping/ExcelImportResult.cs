#nullable enable

using DataAccessLayer.Entities;

namespace BusinessLayer.Mapping
{
    public class ExcelImportResult
    {
        public ExcelImportResult()
        {
            SubjectClasses = new List<SubjectClass>();
            Students = new List<Student>();
            Components = new List<GradingComponent>();
            Marks = new List<Mark>();
        }

        public List<SubjectClass> SubjectClasses { get; }
        public List<Student> Students { get; }
        public List<GradingComponent> Components { get; }
        public List<Mark> Marks { get; }
    }
}
