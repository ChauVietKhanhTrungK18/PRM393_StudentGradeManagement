#nullable enable

using BusinessLayer.DTOs;
using BusinessLayer.IService;
using BusinessLayer.Mapping;
using DataAccessLayer.DbContexts;
using DataAccessLayer.Entities;
using DataAccessLayer.FileHandlers.Excel;
using Microsoft.EntityFrameworkCore;
using DbComponent = DataAccessLayer.Entities.GradingComponent;
using DbStudent = DataAccessLayer.Entities.Student;

namespace BusinessLayer.Services
{
    public class ExcelImportService : IExcelImportService
    {
        private const int PreviewRowLimit = 100;

        private readonly IExcelReader _reader;
        private readonly ExcelMapper _mapper;
        private readonly AppDbContext _dbContext;

        public ExcelImportService(
            IExcelReader reader,
            ExcelMapper mapper,
            AppDbContext dbContext)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<ExcelReadResultDto> ReadWorkbookAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var workbook =
                await _reader.GetWorkbookInfoAsync(filePath, cancellationToken)
                    .ConfigureAwait(false);

            return new ExcelReadResultDto
            {
                Sheets = workbook.Sheets
                    .Select(s => new ExcelSheetInfoDto
                    {
                        Name = s.Name,
                        Headers = s.Headers
                    })
                    .ToList()
            };
        }

        public async Task<ExcelPreviewResultDto> PreviewAsync(
            string filePath,
            ExcelColumnMappingDto mapping,
            CancellationToken cancellationToken = default)
        {
            ValidateMapping(mapping);

            var rows =
                await _reader.ReadSheetRowsAsync(
                        filePath,
                        mapping.SheetName,
                        cancellationToken)
                    .ConfigureAwait(false);

            var previewRows = rows
                .Select(row => _mapper.MapPreviewRow(row, mapping))
                .ToList();

            var validRows = previewRows.Count(r => r.IsValid);
            var warnings = new List<string>();

            if (rows.Count > PreviewRowLimit)
            {
                warnings.Add(
                    $"Preview limited to first {PreviewRowLimit} data rows.");
            }

            return new ExcelPreviewResultDto
            {
                TotalRows = previewRows.Count,
                ValidRows = validRows,
                InvalidRows = previewRows.Count - validRows,
                Rows = previewRows.Take(PreviewRowLimit).ToList(),
                Warnings = warnings
            };
        }

        public async Task<ExcelImportResultDto> ImportAsync(
            string filePath,
            ExcelColumnMappingDto mapping,
            string changedBy,
            CancellationToken cancellationToken = default)
        {
            ValidateMapping(mapping);

            var log = new List<ExcelImportLogEntryDto>();
            void AddLog(string level, string message)
            {
                log.Add(new ExcelImportLogEntryDto
                {
                    Level = level,
                    Message = message,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }

            var rows =
                await _reader.ReadSheetRowsAsync(
                        filePath,
                        mapping.SheetName,
                        cancellationToken)
                    .ConfigureAwait(false);

            var validRows = rows
                .Where(row => _mapper.MapPreviewRow(row, mapping).IsValid)
                .ToList();

            var skippedRows = rows.Count - validRows.Count;
            if (skippedRows > 0)
            {
                AddLog(
                    "Warning",
                    $"{skippedRows} row(s) skipped due to validation errors.");
            }

            var import = _mapper.MapRows(validRows, mapping);

            await using var tx =
                await _dbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);

            try
            {
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
                        AddLog(
                            "Info",
                            $"Created subject class {sc.SubjectCode} - {sc.ClassName}.");
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

                        AddLog(
                            "Info",
                            $"Reused subject class {sc.SubjectCode} - {sc.ClassName}.");
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
                        AddLog(
                            "Info",
                            $"Created grading component '{comp.Name}'.");
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
                        AddLog(
                            "Info",
                            $"Created student {st.RollNumber} ({st.FullName}).");
                    }
                    else
                    {
                        existingStudent.FullName = st.FullName;
                        existingStudent.Comment = st.Comment;
                        st.Id = existingStudent.Id;

                        AddLog(
                            "Info",
                            $"Updated student {st.RollNumber} ({st.FullName}).");
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                var componentsLookup =
                    await _dbContext.GradingComponents
                        .Include(c => c.SubjectClass)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                var marksWritten = 0;
                foreach (var mark in import.Marks)
                {
                    DbStudent? student =
                        await _dbContext.Students
                            .Include(s => s.SubjectClass)
                            .FirstOrDefaultAsync(
                                s =>
                                    s.RollNumber == mark.Student.RollNumber &&
                                    s.SubjectClass.SubjectCode ==
                                    mark.Student.SubjectClass.SubjectCode &&
                                    s.SubjectClass.ClassName ==
                                    mark.Student.SubjectClass.ClassName,
                                cancellationToken)
                            .ConfigureAwait(false);

                    DbComponent? component =
                        componentsLookup.FirstOrDefault(
                            c =>
                                c.Name == mark.Component.Name &&
                                c.SubjectClass.SubjectCode ==
                                mark.Component.SubjectClass.SubjectCode &&
                                c.SubjectClass.ClassName ==
                                mark.Component.SubjectClass.ClassName);

                    if (student == null || component == null)
                        continue;

                    var existingMark =
                        await _dbContext.Marks
                            .FirstOrDefaultAsync(
                                m =>
                                    m.StudentId == student.Id &&
                                    m.ComponentId == component.Id,
                                cancellationToken)
                            .ConfigureAwait(false);

                    if (existingMark == null)
                    {
                        _dbContext.Marks.Add(new Mark
                        {
                            Student = student,
                            Component = component,
                            Value = mark.Value,
                            Comment = mark.Comment
                        });

                        _dbContext.AuditLogs.Add(new AuditLog
                        {
                            Student = student,
                            Component = component,
                            OldValue = null,
                            NewValue = mark.Value,
                            ChangedBy = changedBy,
                            ChangedAt = DateTimeOffset.UtcNow
                        });

                        AddLog(
                            "Info",
                            $"Inserted mark {mark.Value} for {student.RollNumber} / {component.Name}.");
                    }
                    else
                    {
                        var oldValue = existingMark.Value;
                        existingMark.Value = mark.Value;
                        existingMark.Comment = mark.Comment;

                        _dbContext.AuditLogs.Add(new AuditLog
                        {
                            Student = student,
                            Component = component,
                            OldValue = oldValue,
                            NewValue = mark.Value,
                            ChangedBy = changedBy,
                            ChangedAt = DateTimeOffset.UtcNow
                        });

                        AddLog(
                            "Info",
                            $"Overwrote mark for {student.RollNumber} / {component.Name}: {oldValue} -> {mark.Value}.");
                    }

                    marksWritten++;
                }

                await _dbContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                AddLog("Info", "Excel import completed successfully.");

                return new ExcelImportResultDto
                {
                    SubjectClassCount = import.SubjectClasses.Count,
                    StudentCount = import.Students.Count,
                    ComponentCount = import.Components.Count,
                    MarkCount = marksWritten,
                    SkippedRows = skippedRows,
                    ImportLog = log
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken)
                    .ConfigureAwait(false);

                AddLog("Error", ex.Message);
                throw;
            }
        }

        private static void ValidateMapping(ExcelColumnMappingDto mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            if (string.IsNullOrWhiteSpace(mapping.SheetName))
                throw new InvalidOperationException("Sheet name is required.");

            if (string.IsNullOrWhiteSpace(mapping.RollNumberColumn))
                throw new InvalidOperationException("Roll number column mapping is required.");

            if (string.IsNullOrWhiteSpace(mapping.FullNameColumn))
                throw new InvalidOperationException("Full name column mapping is required.");

            var hasSubjectCode =
                !string.IsNullOrWhiteSpace(mapping.SubjectCode) ||
                !string.IsNullOrWhiteSpace(mapping.SubjectCodeColumn);

            var hasClassName =
                !string.IsNullOrWhiteSpace(mapping.ClassName) ||
                !string.IsNullOrWhiteSpace(mapping.ClassNameColumn);

            if (!hasSubjectCode)
                throw new InvalidOperationException(
                    "Subject code must be provided as a fixed value or column mapping.");

            if (!hasClassName)
                throw new InvalidOperationException(
                    "Class name must be provided as a fixed value or column mapping.");
        }
    }
}
