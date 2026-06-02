using DataAccessLayer.DbContexts;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repository
{
    public class ExcelImportRepository : IExcelImportRepository
    {
        private readonly AppDbContext _db;

        public ExcelImportRepository(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<SubjectClass?> GetSubjectClassWithGradesAsync(
            string subjectCode,
            string className,
            CancellationToken cancellationToken = default)
        {
            return await _db.SubjectClasses
                .AsNoTracking()
                .Include(sc => sc.Students)
                .ThenInclude(s => s.Marks)
                .Include(sc => sc.GradingComponents)
                .FirstOrDefaultAsync(
                    sc =>
                        sc.SubjectCode == subjectCode &&
                        sc.ClassName == className,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
