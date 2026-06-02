using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepository
{
    public interface IExcelImportRepository
    {
        Task<SubjectClass?> GetSubjectClassWithGradesAsync(
            string subjectCode,
            string className,
            CancellationToken cancellationToken = default);
    }
}
