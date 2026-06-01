#nullable enable

using BusinessLayer.DTOs;
using BusinessLayer.IService;
using DataAccessLayer.DbContexts;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class MarkService : IMarkService
    {
        private readonly AppDbContext _dbContext;

        public MarkService(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<MarkUpdateResultDto> UpdateMarkAsync(
            MarkUpdateRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.SubjectCode) ||
                string.IsNullOrWhiteSpace(request.ClassName) ||
                string.IsNullOrWhiteSpace(request.RollNumber) ||
                string.IsNullOrWhiteSpace(request.ComponentName))
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Missing required identifiers.",
                    Value = 0m
                };
            }

            var subjectClass = await _dbContext.SubjectClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    sc => sc.SubjectCode == request.SubjectCode &&
                          sc.ClassName == request.ClassName,
                    cancellationToken);

            if (subjectClass == null)
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Subject class not found.",
                    Value = 0m
                };
            }

            var student = await _dbContext.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.SubjectClassId == subjectClass.Id &&
                         s.RollNumber == request.RollNumber,
                    cancellationToken);

            if (student == null)
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Student not found.",
                    Value = 0m
                };
            }

            var component = await _dbContext.GradingComponents
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.SubjectClassId == subjectClass.Id &&
                         c.Name == request.ComponentName,
                    cancellationToken);

            if (component == null)
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Component not found.",
                    Value = 0m
                };
            }

            if (!TryParseScore(request.RawValue, out var value, out var parseMessage))
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = parseMessage,
                    Value = 0m
                };
            }

            if (value < 0m)
            {
                return new MarkUpdateResultDto
                {
                    Success = true,
                    IsValid = false,
                    Message = "Score must be >= 0.",
                    Value = value
                };
            }

            if (value > 10 && value != null)
            {
                return new MarkUpdateResultDto
                {
                    Success = true,
                    IsValid = false,
                    Message = $"Score must be <= 10.",
                    Value = value
                };
            }

            if (DecimalScale(value) > 3)
            {
                return new MarkUpdateResultDto
                {
                    Success = true,
                    IsValid = false,
                    Message = "Score supports up to 3 decimal places.",
                    Value = value
                };
            }

            await using var tx = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var existingMark = await _dbContext.Marks
                    .FirstOrDefaultAsync(
                        m => m.StudentId == student.Id &&
                             m.ComponentId == component.Id,
                        cancellationToken);

                if (existingMark == null)
                {
                    _dbContext.Marks.Add(new Mark
                    {
                        StudentId = student.Id,
                        ComponentId = component.Id,
                        Value = value,
                        Comment = null
                    });
                }
                else
                {
                    existingMark.Value = value;
                }

                await _dbContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new MarkUpdateResultDto
                {
                    Success = true,
                    IsValid = true,
                    Message = "OK",
                    Value = value
                };
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken)
                    .ConfigureAwait(false);

                throw;
            }
        }

        public async Task<MarkBulkUpdateResultDto> UpdateMarksAsync(
            MarkBulkUpdateRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.SubjectCode) ||
                string.IsNullOrWhiteSpace(request.ClassName) ||
                string.IsNullOrWhiteSpace(request.RollNumber))
            {
                return new MarkBulkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Missing required identifiers.",
                    Results = new Dictionary<string, MarkCellResultDto>()
                };
            }

            var subjectClass = await _dbContext.SubjectClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    sc => sc.SubjectCode == request.SubjectCode &&
                          sc.ClassName == request.ClassName,
                    cancellationToken);

            if (subjectClass == null)
            {
                return new MarkBulkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Subject class not found.",
                    Results = new Dictionary<string, MarkCellResultDto>()
                };
            }

            var student = await _dbContext.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.SubjectClassId == subjectClass.Id &&
                         s.RollNumber == request.RollNumber,
                    cancellationToken);

            if (student == null)
            {
                return new MarkBulkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Student not found.",
                    Results = new Dictionary<string, MarkCellResultDto>()
                };
            }

            var components = await _dbContext.GradingComponents
                .AsNoTracking()
                .Where(c => c.SubjectClassId == subjectClass.Id)
                .ToListAsync(cancellationToken);

            var componentNames = components
                .Select(c => c.Name)
                .ToList();

            var requestNames = request.Marks?.Keys.ToList()
                ?? new List<string>();

            var extra = requestNames
                .Where(r => !componentNames.Contains(r, StringComparer.Ordinal))
                .ToList();

            if (extra.Count > 0)
            {
                var message = $"Unknown components: {string.Join(", ", extra)}.";

                return new MarkBulkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = message,
                    Results = new Dictionary<string, MarkCellResultDto>()
                };
            }

            if (requestNames.Count == 0)
            {
                return new MarkBulkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "No marks provided.",
                    Results = new Dictionary<string, MarkCellResultDto>()
                };
            }

            var requestedComponents = components
                .Where(c => requestNames.Contains(c.Name, StringComparer.Ordinal))
                .ToList();

            var results = new Dictionary<string, MarkCellResultDto>();
            var allValid = true;

            foreach (var component in requestedComponents)
            {
                var rawValue = request.Marks[component.Name];
                if (!TryParseScore(rawValue, out var value, out var parseMessage))
                {
                    results[component.Name] = new MarkCellResultDto
                    {
                        IsValid = false,
                        Message = parseMessage,
                        Value = 0m
                    };
                    allValid = false;
                    continue;
                }

                if (value < 0m)
                {
                    results[component.Name] = new MarkCellResultDto
                    {
                        IsValid = false,
                        Message = "Score must be >= 0.",
                        Value = value
                    };
                    allValid = false;
                    continue;
                }

                if (value != null && value > 10)
                {
                    results[component.Name] = new MarkCellResultDto
                    {
                        IsValid = false,
                        Message = $"Score must be <= 10.",
                        Value = value
                    };
                    allValid = false;
                    continue;
                }

                if (DecimalScale(value) > 3)
                {
                    results[component.Name] = new MarkCellResultDto
                    {
                        IsValid = false,
                        Message = "Score supports up to 3 decimal places.",
                        Value = value
                    };
                    allValid = false;
                    continue;
                }

                results[component.Name] = new MarkCellResultDto
                {
                    IsValid = true,
                    Message = "OK",
                    Value = value
                };
            }

            if (!allValid)
            {
                return new MarkBulkUpdateResultDto
                {
                    Success = true,
                    IsValid = false,
                    Message = "Validation failed.",
                    Results = results
                };
            }

            var componentIdLookup = requestedComponents
                .ToDictionary(c => c.Name, c => c.Id, StringComparer.Ordinal);

            var componentIds = componentIdLookup.Values.ToList();

            await using var tx = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var existingMarks = await _dbContext.Marks
                    .Where(m => m.StudentId == student.Id && componentIds.Contains(m.ComponentId))
                    .ToListAsync(cancellationToken);

                foreach (var component in requestedComponents)
                {
                    var value = results[component.Name].Value;
                    var existingMark = existingMarks
                        .FirstOrDefault(m => m.ComponentId == component.Id);

                    if (existingMark == null)
                    {
                        _dbContext.Marks.Add(new Mark
                        {
                            StudentId = student.Id,
                            ComponentId = component.Id,
                            Value = value,
                            Comment = null
                        });
                    }
                    else
                    {
                        existingMark.Value = value;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new MarkBulkUpdateResultDto
                {
                    Success = true,
                    IsValid = true,
                    Message = "OK",
                    Results = results
                };
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken)
                    .ConfigureAwait(false);

                throw;
            }
        }

        public async Task<MarkUpdateResultDto> ClearMarkAsync(
            MarkClearByNameRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.SubjectCode) ||
                string.IsNullOrWhiteSpace(request.ClassName) ||
                string.IsNullOrWhiteSpace(request.RollNumber) ||
                string.IsNullOrWhiteSpace(request.ComponentName))
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Missing required identifiers.",
                    Value = 0m
                };
            }

            var subjectClass = await _dbContext.SubjectClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    sc => sc.SubjectCode == request.SubjectCode &&
                          sc.ClassName == request.ClassName,
                    cancellationToken);

            if (subjectClass == null)
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Subject class not found.",
                    Value = 0m
                };
            }

            var student = await _dbContext.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.SubjectClassId == subjectClass.Id &&
                         s.RollNumber == request.RollNumber,
                    cancellationToken);

            if (student == null)
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Student not found.",
                    Value = 0m
                };
            }

            var component = await _dbContext.GradingComponents
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.SubjectClassId == subjectClass.Id &&
                         c.Name == request.ComponentName,
                    cancellationToken);

            if (component == null)
            {
                return new MarkUpdateResultDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Component not found.",
                    Value = 0m
                };
            }

            await using var tx = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var existingMark = await _dbContext.Marks
                    .FirstOrDefaultAsync(
                        m => m.StudentId == student.Id &&
                             m.ComponentId == component.Id,
                        cancellationToken);

                if (existingMark != null)
                {
                    _dbContext.Marks.Remove(existingMark);

                    await _dbContext.SaveChangesAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new MarkUpdateResultDto
                {
                    Success = true,
                    IsValid = true,
                    Message = existingMark == null ? "Already clear." : "Cleared.",
                    Value = 0m
                };
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken)
                    .ConfigureAwait(false);

                throw;
            }
        }

        private static bool TryParseScore(
            string? rawValue,
            out decimal value,
            out string message)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                value = 0m;
                message = "OK";
                return true;
            }

            if (decimal.TryParse(
                    rawValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                message = "OK";
                return true;
            }

            if (decimal.TryParse(
                    rawValue,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out value))
            {
                message = "OK";
                return true;
            }

            value = 0m;
            message = "Invalid numeric value.";
            return false;
        }

        private static int DecimalScale(decimal value)
        {
            var bits = decimal.GetBits(value);
            return (bits[3] >> 16) & 0x7F;
        }
    }
}
