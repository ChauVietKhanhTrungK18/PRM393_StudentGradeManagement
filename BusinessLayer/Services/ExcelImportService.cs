#nullable enable

using BusinessLayer.DTOs;
using BusinessLayer.IService;
using BusinessLayer.Mapping;
using DataAccessLayer.DbContexts;
using DataAccessLayer.Entities;
using DataAccessLayer.FileHandlers.Excel;
using DataAccessLayer.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ExcelImportService : IExcelImportService
    {
        private readonly IExcelReader _reader;
        private readonly IExcelUploadStore _uploadStore;
        private readonly IExcelImportRepository _importRepository;
        private readonly ExcelMapper _mapper;
        private readonly AppDbContext _dbContext;

        public ExcelImportService(
            IExcelReader reader,
            IExcelUploadStore uploadStore,
            IExcelImportRepository importRepository,
            ExcelMapper mapper,
            AppDbContext dbContext)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _uploadStore = uploadStore ?? throw new ArgumentNullException(nameof(uploadStore));
            _importRepository = importRepository
                ?? throw new ArgumentNullException(nameof(importRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<ExcelUploadResultDto> UploadAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            ValidateUploadFile(file);

            await using var stream = file.OpenReadStream();
            var relativePath =
                await _uploadStore.SaveAsync(stream, cancellationToken)
                    .ConfigureAwait(false);

            var physicalPath = _uploadStore.GetPhysicalPath(relativePath);
            var workbook =
                await _reader.GetWorkbookInfoAsync(physicalPath, cancellationToken)
                    .ConfigureAwait(false);

            return new ExcelUploadResultDto
            {
                FilePath = relativePath,
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
            ExcelPreviewRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidatePreviewImportRequest(request);

            var subjectClass =
                await _importRepository.GetSubjectClassWithGradesAsync(
                        request.SubjectCode,
                        request.ClassName,
                        cancellationToken)
                    .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Subject class {request.SubjectCode} - {request.ClassName} was not found in the database.");

            var rows = await ReadMappedRowsAsync(
                    request.FilePath,
                    request.SheetName,
                    request.ColumnMapping,
                    cancellationToken)
                .ConfigureAwait(false);

            var studentsByRoll = subjectClass.Students
                .ToDictionary(s => s.RollNumber, StringComparer.OrdinalIgnoreCase);

            var componentsByName = subjectClass.GradingComponents
                .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            var preview = new List<ExcelStudentPreviewDto>();
            var notFoundRolls = new List<string>();

            foreach (var parsed in rows.ParsedRows)
            {
                if (!studentsByRoll.TryGetValue(parsed.RollNumber, out var student))
                {
                    if (!notFoundRolls.Contains(parsed.RollNumber, StringComparer.OrdinalIgnoreCase))
                        notFoundRolls.Add(parsed.RollNumber);
                    continue;
                }

                var studentPreview = new ExcelStudentPreviewDto
                {
                    RollNumber = parsed.RollNumber,
                    FullName = string.IsNullOrWhiteSpace(parsed.FullName)
                        ? student.FullName
                        : parsed.FullName.Trim()
                };

                foreach (var markMapping in request.ColumnMapping.Marks)
                {
                    if (string.IsNullOrWhiteSpace(markMapping.ComponentName))
                        continue;

                    var componentName = markMapping.ComponentName.Trim();
                    if (!componentsByName.TryGetValue(componentName, out var component))
                        continue;

                    decimal? currentValue = student.Marks
                        .FirstOrDefault(m => m.ComponentId == component.Id)
                        ?.Value;

                    parsed.Marks.TryGetValue(componentName, out var newValue);

                    studentPreview.Marks.Add(new ExcelMarkPreviewDto
                    {
                        ComponentName = componentName,
                        CurrentValue = currentValue,
                        NewValue = parsed.Marks.ContainsKey(componentName)
                            ? newValue
                            : null
                    });
                }

                if (studentPreview.Marks.Count > 0)
                    preview.Add(studentPreview);
            }

            return new ExcelPreviewResultDto
            {
                Preview = preview,
                NotFoundRolls = notFoundRolls,
                TotalRows = rows.TotalRows,
                ValidRows = preview.Count
            };
        }

        public async Task<ExcelImportResultDto> ImportAsync(
            ExcelImportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateImportRequest(request);

            if (!request.Overwrite)
            {
                return new ExcelImportResultDto
                {
                    Success = false,
                    Errors = new List<string>
                    {
                        "Import requires overwrite=true to apply grade changes."
                    }
                };
            }

            var subjectClass =
                await _dbContext.SubjectClasses
                    .Include(sc => sc.Students)
                    .ThenInclude(s => s.Marks)
                    .Include(sc => sc.GradingComponents)
                    .FirstOrDefaultAsync(
                        sc =>
                            sc.SubjectCode == request.SubjectCode &&
                            sc.ClassName == request.ClassName,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (subjectClass == null)
            {
                return new ExcelImportResultDto
                {
                    Success = false,
                    Errors = new List<string>
                    {
                        $"Subject class {request.SubjectCode} - {request.ClassName} was not found."
                    }
                };
            }

            var rows = await ReadMappedRowsAsync(
                    request.FilePath,
                    request.SheetName,
                    request.ColumnMapping,
                    cancellationToken)
                .ConfigureAwait(false);

            var studentsByRoll = subjectClass.Students
                .ToDictionary(s => s.RollNumber, StringComparer.OrdinalIgnoreCase);

            var componentsByName = subjectClass.GradingComponents
                .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            var notFoundRolls = new List<string>();
            var errors = new List<string>();
            var importedCount = 0;
            var skippedCount = 0;

            await using var tx =
                await _dbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);

            try
            {
                foreach (var parsed in rows.ParsedRows)
                {
                    if (!studentsByRoll.TryGetValue(parsed.RollNumber, out var student))
                    {
                        if (!notFoundRolls.Contains(parsed.RollNumber, StringComparer.OrdinalIgnoreCase))
                            notFoundRolls.Add(parsed.RollNumber);
                        skippedCount++;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(parsed.FullName))
                        student.FullName = parsed.FullName.Trim();

                    if (!string.IsNullOrWhiteSpace(parsed.Comment))
                        student.Comment = parsed.Comment.Trim();

                    var rowImported = false;

                    foreach (var markMapping in request.ColumnMapping.Marks)
                    {
                        if (string.IsNullOrWhiteSpace(markMapping.ComponentName))
                            continue;

                        var componentName = markMapping.ComponentName.Trim();
                        if (!componentsByName.TryGetValue(componentName, out var component))
                        {
                            errors.Add(
                                $"Component '{componentName}' not found for roll {parsed.RollNumber}.");
                            continue;
                        }

                        if (!parsed.Marks.TryGetValue(componentName, out var newValue))
                            continue;

                        var existingMark = student.Marks
                            .FirstOrDefault(m => m.ComponentId == component.Id);

                        if (existingMark == null)
                        {
                            var mark = new Mark
                            {
                                StudentId = student.Id,
                                ComponentId = component.Id,
                                Value = newValue
                            };

                            _dbContext.Marks.Add(mark);
                            student.Marks.Add(mark);

                            _dbContext.AuditLogs.Add(new AuditLog
                            {
                                StudentId = student.Id,
                                ComponentId = component.Id,
                                OldValue = null,
                                NewValue = newValue,
                                ChangedBy = "excel-import",
                                ChangedAt = DateTimeOffset.UtcNow
                            });

                            rowImported = true;
                        }
                        else if (existingMark.Value != newValue)
                        {
                            var oldValue = existingMark.Value;
                            existingMark.Value = newValue;

                            _dbContext.AuditLogs.Add(new AuditLog
                            {
                                StudentId = student.Id,
                                ComponentId = component.Id,
                                OldValue = oldValue,
                                NewValue = newValue,
                                ChangedBy = "excel-import",
                                ChangedAt = DateTimeOffset.UtcNow
                            });

                            rowImported = true;
                        }
                    }

                    if (rowImported)
                        importedCount++;
                    else
                        skippedCount++;
                }

                await _dbContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (rows.ParsedRows.Count == 0 && errors.Count == 0)
                {
                    errors.Add("Không đọc được dòng điểm hợp lệ trong trang tính đã chọn.");
                }
                else if (rows.ParsedRows.Count > 0 &&
                    notFoundRolls.Count == rows.ParsedRows.Count &&
                    errors.Count == 0)
                {
                    errors.Add("Không tìm thấy sinh viên nào trong lớp khớp với cột Roll/MSSV của Excel.");
                }

                return new ExcelImportResultDto
                {
                    Success = errors.Count == 0,
                    ImportedCount = importedCount,
                    SkippedCount = skippedCount,
                    NotFoundRolls = notFoundRolls,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);

                return new ExcelImportResultDto
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ExcelSyncResultDto> SyncAllSheetsAsync(
            ExcelSyncRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.FilePath))
                throw new InvalidOperationException("filePath is required.");

            var physicalPath = ResolveUploadedFile(request.FilePath);
            var workbook =
                await _reader.GetWorkbookInfoAsync(physicalPath, cancellationToken)
                    .ConfigureAwait(false);

            var subjectClasses =
                await _dbContext.SubjectClasses
                    .Include(sc => sc.Students)
                    .ThenInclude(s => s.Marks)
                    .Include(sc => sc.GradingComponents)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

            var sheetResults = new List<ExcelSheetSyncResultDto>();
            var importedSheets = 0;
            var skippedSheets = 0;

            foreach (var sheet in workbook.Sheets)
            {
                var sheetResult =
                    await SyncSingleSheetAsync(
                            request.FilePath,
                            sheet,
                            subjectClasses,
                            cancellationToken)
                        .ConfigureAwait(false);

                sheetResults.Add(sheetResult);

                if (sheetResult.Status == "imported")
                    importedSheets++;
                else if (sheetResult.Status == "skipped")
                    skippedSheets++;
            }

            return new ExcelSyncResultDto
            {
                FilePath = request.FilePath,
                Success = importedSheets > 0,
                TotalSheets = workbook.Sheets.Count,
                ImportedSheets = importedSheets,
                SkippedSheets = skippedSheets,
                Sheets = sheetResults
            };
        }

        public async Task<ExcelSyncResultDto> UploadAndSyncAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var upload = await UploadAsync(file, cancellationToken).ConfigureAwait(false);

            return await SyncAllSheetsAsync(
                    new ExcelSyncRequestDto { FilePath = upload.FilePath },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<ExcelSheetSyncResultDto> SyncSingleSheetAsync(
            string filePath,
            ExcelSheetInfo sheet,
            List<SubjectClass> subjectClasses,
            CancellationToken cancellationToken)
        {
            var result = new ExcelSheetSyncResultDto
            {
                SheetName = sheet.Name
            };

            if (sheet.Headers.Count == 0)
            {
                result.Status = "skipped";
                result.Message = "Sheet has no header row.";
                return result;
            }

            var subjectClass = ExcelWorksheetNaming.MatchSheet(sheet.Name, subjectClasses);
            if (subjectClass == null)
            {
                result.Status = "skipped";
                result.Message = "No matching subject/class in database for this sheet name.";
                return result;
            }

            result.SubjectCode = subjectClass.SubjectCode;
            result.ClassName = subjectClass.ClassName;

            ExcelColumnMappingDto columnMapping;
            try
            {
                columnMapping = ExcelAutoMappingBuilder.Build(
                    sheet.Headers,
                    subjectClass.GradingComponents.Select(c => c.Name));
            }
            catch (Exception ex)
            {
                result.Status = "skipped";
                result.Message = ex.Message;
                return result;
            }

            var rows =
                await ReadMappedRowsAsync(
                        filePath,
                        sheet.Name,
                        columnMapping,
                        cancellationToken)
                    .ConfigureAwait(false);

            var studentsByRoll = subjectClass.Students
                .ToDictionary(s => s.RollNumber, StringComparer.OrdinalIgnoreCase);

            var componentsByName = subjectClass.GradingComponents
                .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            var notFoundRolls = new List<string>();
            var changedMarks = 0;
            var marksToApply = new List<(Student Student, GradingComponent Component, decimal NewValue)>();

            foreach (var parsed in rows.ParsedRows)
            {
                if (!studentsByRoll.TryGetValue(parsed.RollNumber, out var student))
                {
                    if (!notFoundRolls.Contains(parsed.RollNumber, StringComparer.OrdinalIgnoreCase))
                        notFoundRolls.Add(parsed.RollNumber);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(parsed.FullName))
                    student.FullName = parsed.FullName.Trim();

                if (!string.IsNullOrWhiteSpace(parsed.Comment))
                    student.Comment = parsed.Comment.Trim();

                foreach (var markMapping in columnMapping.Marks)
                {
                    var componentName = markMapping.ComponentName.Trim();
                    if (!componentsByName.TryGetValue(componentName, out var component))
                        continue;

                    if (!parsed.Marks.TryGetValue(componentName, out var newValue))
                        continue;

                    var existingMark = student.Marks
                        .FirstOrDefault(m => m.ComponentId == component.Id);

                    var currentValue = existingMark?.Value;
                    if (currentValue == newValue)
                        continue;

                    changedMarks++;
                    marksToApply.Add((student, component, newValue));
                }
            }

            result.NotFoundRolls = notFoundRolls;
            result.ChangedMarksPreview = changedMarks;

            if (changedMarks == 0)
            {
                result.Status = "no_changes";
                result.Message = "Excel grades match the database (no updates needed).";
                return result;
            }

            var updatedStudents = new HashSet<int>();
            var updatedMarks = 0;

            foreach (var (student, component, newValue) in marksToApply)
            {
                var existingMark = student.Marks
                    .FirstOrDefault(m => m.ComponentId == component.Id);

                if (existingMark == null)
                {
                    var mark = new Mark
                    {
                        StudentId = student.Id,
                        ComponentId = component.Id,
                        Value = newValue
                    };

                    _dbContext.Marks.Add(mark);
                    student.Marks.Add(mark);

                    _dbContext.AuditLogs.Add(new AuditLog
                    {
                        StudentId = student.Id,
                        ComponentId = component.Id,
                        OldValue = null,
                        NewValue = newValue,
                        ChangedBy = "excel-sync",
                        ChangedAt = DateTimeOffset.UtcNow
                    });
                }
                else
                {
                    var oldValue = existingMark.Value;
                    existingMark.Value = newValue;

                    _dbContext.AuditLogs.Add(new AuditLog
                    {
                        StudentId = student.Id,
                        ComponentId = component.Id,
                        OldValue = oldValue,
                        NewValue = newValue,
                        ChangedBy = "excel-sync",
                        ChangedAt = DateTimeOffset.UtcNow
                    });
                }

                updatedStudents.Add(student.Id);
                updatedMarks++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            result.Status = "imported";
            result.Message = $"Updated {updatedMarks} mark(s) for {updatedStudents.Count} student(s).";
            result.UpdatedStudents = updatedStudents.Count;
            result.UpdatedMarks = updatedMarks;

            return result;
        }

        private async Task<SheetParseResult> ReadMappedRowsAsync(
            string filePath,
            string sheetName,
            ExcelColumnMappingDto columnMapping,
            CancellationToken cancellationToken)
        {
            var physicalPath = ResolveUploadedFile(filePath);

            var excelRows =
                await _reader.ReadSheetRowsAsync(
                        physicalPath,
                        sheetName,
                        cancellationToken)
                    .ConfigureAwait(false);

            var parsedRows = new List<ParsedExcelRow>();
            foreach (var row in excelRows)
            {
                var parsed = _mapper.ParseRow(row, columnMapping);
                if (parsed != null)
                    parsedRows.Add(parsed);
            }

            return new SheetParseResult
            {
                TotalRows = excelRows.Count,
                ParsedRows = parsedRows
            };
        }

        private string ResolveUploadedFile(string relativeFilePath)
        {
            if (!_uploadStore.Exists(relativeFilePath))
            {
                throw new FileNotFoundException(
                    "Uploaded file was not found. Please upload the Excel file again.");
            }

            return _uploadStore.GetPhysicalPath(relativeFilePath);
        }

        private static void ValidateUploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file provided.");

            if (!string.Equals(
                    Path.GetExtension(file.FileName),
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only .xlsx files are allowed.");
            }
        }

        private static void ValidatePreviewImportRequest(ExcelPreviewRequestDto request)
        {
            ValidateCoreRequest(
                request.FilePath,
                request.SheetName,
                request.SubjectCode,
                request.ClassName,
                request.ColumnMapping);
        }

        private static void ValidateImportRequest(ExcelImportRequestDto request)
        {
            ValidateCoreRequest(
                request.FilePath,
                request.SheetName,
                request.SubjectCode,
                request.ClassName,
                request.ColumnMapping);
        }

        private static void ValidateCoreRequest(
            string filePath,
            string sheetName,
            string subjectCode,
            string className,
            ExcelColumnMappingDto columnMapping)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new InvalidOperationException("filePath is required.");

            if (string.IsNullOrWhiteSpace(sheetName))
                throw new InvalidOperationException("sheetName is required.");

            if (string.IsNullOrWhiteSpace(subjectCode))
                throw new InvalidOperationException("subjectCode is required.");

            if (string.IsNullOrWhiteSpace(className))
                throw new InvalidOperationException("className is required.");

            if (string.IsNullOrWhiteSpace(columnMapping.RollNumberColumn))
                throw new InvalidOperationException("columnMapping.rollNumberColumn is required.");

            if (columnMapping.Marks == null || columnMapping.Marks.Count == 0)
                throw new InvalidOperationException("columnMapping.marks is required.");
        }

        private sealed class SheetParseResult
        {
            public int TotalRows { get; init; }

            public List<ParsedExcelRow> ParsedRows { get; init; } = new();
        }
    }
}
