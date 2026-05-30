using BusinessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.IService
{
    public interface IFGImportService
    {
        Task<FGImportResultDto> ImportFromFileAsync(
            string fgPath,
            CancellationToken cancellationToken = default);
    }
}
