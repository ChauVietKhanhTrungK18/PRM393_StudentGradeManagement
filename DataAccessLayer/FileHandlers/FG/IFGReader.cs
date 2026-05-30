using System.Threading;
using System.Threading.Tasks;
using FuGradeLib;

namespace DataAccessLayer.FileHandlers.FG
{

    public interface IFGReader
    {
        Task<TeacherGrade?> ReadAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
