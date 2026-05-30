#nullable enable

using BusinessLayer.DTOs;
using BusinessLayer.IService;
using DataAccessLayer.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly AppDbContext _dbContext;

        public SubjectService(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<SubjectListItemDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.SubjectClasses
                .AsNoTracking()
                .OrderBy(sc => sc.SubjectCode)
                .ThenBy(sc => sc.ClassName)
                .Select(sc => new SubjectListItemDto
                {
                    SubjectCode = sc.SubjectCode,
                    ClassName = sc.ClassName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<SubjectGradeTableDto?> GetGradeTableAsync(
            string subjectCode,
            string className,
            CancellationToken cancellationToken = default)
        {
            var subjectClass = await _dbContext.SubjectClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    sc => sc.SubjectCode == subjectCode &&
                          sc.ClassName == className,
                    cancellationToken);

            if (subjectClass == null)
                return null;

            var components = await _dbContext.GradingComponents
                .AsNoTracking()
                .Where(c => c.SubjectClassId == subjectClass.Id)
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync(cancellationToken);

            var students = await _dbContext.Students
                .AsNoTracking()
                .Where(s => s.SubjectClassId == subjectClass.Id)
                .OrderBy(s => s.RollNumber)
                .Select(s => new
                {
                    s.RollNumber,
                    s.FullName,
                    s.Comment,
                    Marks = s.Marks.Select(m => new
                    {
                        ComponentName = m.Component.Name,
                        m.Value
                    })
                })
                .ToListAsync(cancellationToken);

            var studentRows = students.Select(s => new StudentGradeRowDto
            {
                RollNumber = s.RollNumber,
                FullName = s.FullName,
                Comment = s.Comment,
                Marks = components.ToDictionary(
                    comp => comp,
                    comp => (decimal?)s.Marks
                        .FirstOrDefault(m => m.ComponentName == comp)
                        ?.Value)
            }).ToList();

            return new SubjectGradeTableDto
            {
                SubjectCode = subjectClass.SubjectCode,
                ClassName = subjectClass.ClassName,
                Components = components,
                Students = studentRows
            };
        }
    }
}
