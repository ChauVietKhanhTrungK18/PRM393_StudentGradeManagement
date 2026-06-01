using BusinessLayer.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLayer.IService
{
    public interface ISubjectService
    {
        Task<List<SubjectListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<SubjectGradeTableDto?> GetGradeTableAsync(
            string subjectCode,
            string className,
            CancellationToken cancellationToken = default);
    }
}
