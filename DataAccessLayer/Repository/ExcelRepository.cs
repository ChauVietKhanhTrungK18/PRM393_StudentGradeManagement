using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.DbContexts;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repository
{
    public class ExcelRepository : IExcelRepository
    {
        private readonly AppDbContext _db;

        public ExcelRepository(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<List<SubjectClass>> GetAllForExportAsync(CancellationToken cancellationToken = default)
        {
            var list = await _db.SubjectClasses
                .AsNoTracking()
                .Include(sc => sc.Students!)
                    .ThenInclude(s => s.Marks!)
                .Include(sc => sc.GradingComponents!)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return list;
        }
    }
}
