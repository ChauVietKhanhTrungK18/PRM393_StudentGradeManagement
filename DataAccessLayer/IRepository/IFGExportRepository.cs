#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepository
{
    /// <summary>
    /// Read-only repository that returns SubjectClass entities with navigation properties
    /// needed for FG export.
    /// </summary>
    public interface IFGExportRepository
    {
        Task<List<SubjectClass>> GetAllForExportAsync(CancellationToken cancellationToken = default);
    }
}
