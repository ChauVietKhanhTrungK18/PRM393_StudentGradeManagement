#nullable enable

using BusinessLayer.DTOs;
using BusinessLayer.IService;
using BusinessLayer.Mapping;
using DataAccessLayer.DbContexts;
using DataAccessLayer.FileHandlers.FG;
using FuGradeLib;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DbComponent = DataAccessLayer.Entities.GradingComponent;
using DbStudent = DataAccessLayer.Entities.Student;

namespace BusinessLayer.Services
{
    public class FGImportService : IFGImportService
    {
        private readonly IFGReader _reader;
        private readonly FGMapper _mapper;
        private readonly AppDbContext _dbContext;

        public FGImportService(
            IFGReader reader,
            FGMapper mapper,
            AppDbContext dbContext)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<FGImportResultDto> ImportFromFileAsync(
            string fgPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fgPath))
                throw new ArgumentNullException(nameof(fgPath));

            TeacherGrade? teacherGrade =
                await _reader.ReadAsync(
                    fgPath,
                    cancellationToken)
                .ConfigureAwait(false);

            if (teacherGrade == null)
            {
                throw new InvalidOperationException(
                    "Failed to deserialize TeacherGrade from FG file.");
            }

            var import =
                _mapper.MapTeacherGrade(
                    teacherGrade,
                    fgPath);

            await using var tx =
                await _dbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);

            try
            {
                foreach (var session in import.Sessions)
                {
                    _dbContext.Sessions.Add(session);
                }

                foreach (var sc in import.SubjectClasses)
                {
                    var existing =
                        await _dbContext.SubjectClasses
                            .FirstOrDefaultAsync(
                                x =>
                                    x.SubjectCode == sc.SubjectCode &&
                                    x.ClassName == sc.ClassName,
                                cancellationToken)
                            .ConfigureAwait(false);

                    if (existing == null)
                    {
                        _dbContext.SubjectClasses.Add(sc);
                    }
                    else
                    {
                        foreach (var comp in import.Components
                                     .Where(c => c.SubjectClass == sc))
                        {
                            comp.SubjectClass = existing;
                        }

                        foreach (var student in import.Students
                                     .Where(s => s.SubjectClass == sc))
                        {
                            student.SubjectClass = existing;
                        }
                    }
                }

                foreach (var comp in import.Components)
                {
                    var existingComp =
                        await _dbContext.GradingComponents
                            .Include(c => c.SubjectClass)
                            .FirstOrDefaultAsync(
                                c =>
                                    c.Name == comp.Name &&
                                    c.SubjectClass.SubjectCode ==
                                    comp.SubjectClass.SubjectCode &&
                                    c.SubjectClass.ClassName ==
                                    comp.SubjectClass.ClassName,
                                cancellationToken)
                            .ConfigureAwait(false);

                    if (existingComp == null)
                    {
                        _dbContext.GradingComponents.Add(comp);
                    }
                }

                foreach (var st in import.Students)
                {
                    var existingStudent =
                        await _dbContext.Students
                            .Include(s => s.SubjectClass)
                            .FirstOrDefaultAsync(
                                s =>
                                    s.RollNumber == st.RollNumber &&
                                    s.SubjectClass.SubjectCode ==
                                    st.SubjectClass.SubjectCode &&
                                    s.SubjectClass.ClassName ==
                                    st.SubjectClass.ClassName,
                                cancellationToken)
                            .ConfigureAwait(false);

                    if (existingStudent == null)
                    {
                        _dbContext.Students.Add(st);
                    }
                    else
                    {
                        existingStudent.FullName = st.FullName;
                        existingStudent.Comment = st.Comment;

                        st.Id = existingStudent.Id;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                var componentsLookup =
                    await _dbContext.GradingComponents
                        .Include(c => c.SubjectClass)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                foreach (var mark in import.Marks)
                {
                    DbStudent? student = null;


                    student =
                        await _dbContext.Students
                            .Include(s => s.SubjectClass)
                            .FirstOrDefaultAsync(
                                s =>
                                    s.RollNumber ==
                                    mark.Student.RollNumber &&
                                    s.SubjectClass.SubjectCode ==
                                    mark.Student.SubjectClass.SubjectCode &&
                                    s.SubjectClass.ClassName ==
                                    mark.Student.SubjectClass.ClassName,
                                cancellationToken)
                            .ConfigureAwait(false);

                    DbComponent? component = null;

                    component =
                        componentsLookup.FirstOrDefault(
                            c =>
                                c.Name ==
                                mark.Component.Name &&
                                c.SubjectClass.SubjectCode ==
                                mark.Component.SubjectClass.SubjectCode &&
                                c.SubjectClass.ClassName ==
                                mark.Component.SubjectClass.ClassName);

                    if (student == null || component == null)
                    {
                        continue;
                    }

                    var existingMark = await _dbContext.Marks
                        .FirstOrDefaultAsync(
                            m => m.StudentId == student.Id
                              && m.ComponentId == component.Id,
                            cancellationToken);

                    if (existingMark == null)
                    {
                        _dbContext.Marks.Add(new DataAccessLayer.Entities.Mark
                        {
                            Student = student,
                            Component = component,
                            Value = mark.Value,
                            Comment = mark.Comment
                        });
                    }
                    else
                    {
                        existingMark.Value = mark.Value;
                        existingMark.Comment = mark.Comment;
                    }
                }

                var affected =
                    await _dbContext.SaveChangesAsync(cancellationToken)
                        .ConfigureAwait(false);

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new FGImportResultDto
                {
                    SubjectClassCount = import.SubjectClasses.Count,
                    StudentCount = import.Students.Count,
                    ComponentCount = import.Components.Count,
                    MarkCount = import.Marks.Count
                };
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken)
                    .ConfigureAwait(false);

                throw;
            }
        }
    }
}