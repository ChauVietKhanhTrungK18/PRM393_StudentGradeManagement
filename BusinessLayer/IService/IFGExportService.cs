#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLayer.IService
{

    public interface IFGExportService
    {
        /// <summary>
        /// Export DB data to .fg file bytes.
        /// </summary>
        Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);
    }
}
