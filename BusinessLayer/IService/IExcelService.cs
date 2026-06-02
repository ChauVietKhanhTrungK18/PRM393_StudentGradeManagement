using System.Threading;
using System.Threading.Tasks;

namespace BusinessLayer.IService
{
    public interface IExcelService
    {
        Task<byte[]> ExportTemplateAsync(CancellationToken cancellationToken = default);
    }
}
