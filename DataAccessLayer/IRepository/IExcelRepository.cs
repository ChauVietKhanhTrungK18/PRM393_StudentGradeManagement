using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepository
{
    public interface IExcelRepository
    {
        Task<List<SubjectClass>> GetAllForExportAsync(CancellationToken cancellationToken = default);
    }
}
