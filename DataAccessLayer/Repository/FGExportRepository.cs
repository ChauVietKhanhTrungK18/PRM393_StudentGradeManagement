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
    public class FGExportRepository : IFGExportRepository
    {
        private readonly AppDbContext _db;

        public FGExportRepository(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<List<SubjectClass>> GetAllForExportAsync(CancellationToken cancellationToken = default)
        {
            var list = await _db.SubjectClasses
                .AsNoTracking()
                .Include(sc => sc.GradingComponents)
                .Include(sc => sc.Students)
                    .ThenInclude(s => s.Marks)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return list;
        }
    }
}
