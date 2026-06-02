#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using BusinessLayer.IService;
using DataAccessLayer.IRepository;
using Microsoft.Extensions.Logging;
using FuGradeLib;

namespace BusinessLayer.Services
{
    public class FGExportService : IFGExportService
    {
        private readonly IFGExportRepository _repo;
        private readonly ILogger<FGExportService> _logger;

        public FGExportService(IFGExportRepository repo, ILogger<FGExportService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
        {
            var subjectClasses = await _repo.GetAllForExportAsync(cancellationToken).ConfigureAwait(false);

            var teacher = MapToTeacherGrade(subjectClasses);

#pragma warning disable SYSLIB0011
            try
            {
                return await Task.Run(() =>
                {
                    using var ms = new MemoryStream();
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, teacher);
                    return ms.ToArray();
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình Serialize dữ liệu sang nhị phân.");
                throw;
            }
            finally
            {
#pragma warning restore SYSLIB0011
            }
        }

        private static TeacherGrade MapToTeacherGrade(List<DataAccessLayer.Entities.SubjectClass> subjectClasses)
        {
            var teacher = new TeacherGrade();

            try
            {
                teacher.Login = Environment.UserName ?? string.Empty;
            }
            catch
            {
                teacher.Login = string.Empty;
            }
            teacher.Semester = "SUMMER 2026";

            var scgList = new List<SubjectClassGrade>();

            foreach (var sc in subjectClasses)
            {
                var scg = new SubjectClassGrade
                {
                    Subject = sc.SubjectCode ?? string.Empty,
                    Class = sc.ClassName ?? string.Empty
                };

                var comps = (sc.GradingComponents ?? Array.Empty<DataAccessLayer.Entities.GradingComponent>())
                    .OrderBy(c => c.Name)
                    .ToList();

                scg.Components = comps.Select(c => c.Name ?? string.Empty).ToList();

                var students = (sc.Students ?? Array.Empty<DataAccessLayer.Entities.Student>())
                    .OrderBy(s => s.RollNumber)
                    .ToList();

                var fgStudents = new List<FuGradeLib.Student>();

                foreach (var st in students)
                {
                    var fgSt = new FuGradeLib.Student
                    {
                        Roll = st.RollNumber ?? string.Empty,
                        Name = st.FullName ?? string.Empty,
                        Comment = st.Comment ?? string.Empty
                    };

                    var grades = new List<FuGradeLib.GradeComponent>();

                    var compLookup = comps.ToDictionary(c => c.Id, c => c.Name, EqualityComparer<int>.Default);

                    if (st.Marks != null)
                    {
                        foreach (var m in st.Marks)
                        {
                            if (m == null) continue;

                            var compId = m.ComponentId;
                            if (!compLookup.TryGetValue(compId, out var compName)) continue;

                            float? gval = (float?)Convert.ToSingle(m.Value);

                            var gradeComp = new FuGradeLib.GradeComponent
                            {
                                Component = compName ?? string.Empty,
                                Grade = gval
                            };

                            grades.Add(gradeComp);
                        }
                    }

                    fgSt.Grades = grades;
                    fgStudents.Add(fgSt);
                }

                scg.Students = fgStudents;
                scgList.Add(scg);
            }

            teacher.SubjectClassGrades = scgList;
            return teacher;
        }
    }
}