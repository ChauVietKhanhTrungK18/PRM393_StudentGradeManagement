#nullable enable

using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.FileHandlers.Excel;
using System.Globalization;

namespace BusinessLayer.Mapping
{
    public class ExcelMapper
    {
        public ExcelImportResult MapRows(
            IEnumerable<ExcelRowData> rows,
            ExcelColumnMappingDto mapping)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            var result = new ExcelImportResult();
            var subjectClassLookup =
                new Dictionary<string, SubjectClass>(StringComparer.OrdinalIgnoreCase);
            var componentLookup =
                new Dictionary<string, GradingComponent>(StringComparer.OrdinalIgnoreCase);
            var studentLookup =
                new Dictionary<string, Student>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var subjectCode = ResolveValue(
                    mapping.SubjectCode,
                    mapping.SubjectCodeColumn,
                    row);

                var className = ResolveValue(
                    mapping.ClassName,
                    mapping.ClassNameColumn,
                    row);

                var rollNumber = GetColumnValue(row, mapping.RollNumberColumn);
                var fullName = GetColumnValue(row, mapping.FullNameColumn);
                var comment = string.IsNullOrWhiteSpace(mapping.CommentColumn)
                    ? null
                    : GetColumnValue(row, mapping.CommentColumn);

                if (string.IsNullOrWhiteSpace(subjectCode) ||
                    string.IsNullOrWhiteSpace(className) ||
                    string.IsNullOrWhiteSpace(rollNumber))
                {
                    continue;
                }

                var subjectClassKey = $"{subjectCode}|{className}";
                if (!subjectClassLookup.TryGetValue(subjectClassKey, out var subjectClass))
                {
                    subjectClass = new SubjectClass
                    {
                        SubjectCode = subjectCode.Trim(),
                        ClassName = className.Trim()
                    };

                    subjectClassLookup[subjectClassKey] = subjectClass;
                    result.SubjectClasses.Add(subjectClass);
                }

                foreach (var markMapping in mapping.MarkColumns)
                {
                    if (string.IsNullOrWhiteSpace(markMapping.ComponentName) ||
                        string.IsNullOrWhiteSpace(markMapping.ColumnName))
                    {
                        continue;
                    }

                    var componentKey =
                        $"{subjectClassKey}|{markMapping.ComponentName}";

                    if (!componentLookup.TryGetValue(componentKey, out var component))
                    {
                        component = new GradingComponent
                        {
                            SubjectClass = subjectClass,
                            Name = markMapping.ComponentName.Trim(),
                            MaxMark = 0,
                            Weight = 0,
                            IsCondition = false
                        };

                        componentLookup[componentKey] = component;
                        result.Components.Add(component);
                    }
                }

                var studentKey = $"{subjectClassKey}|{rollNumber.Trim()}";
                if (!studentLookup.TryGetValue(studentKey, out var student))
                {
                    student = new Student
                    {
                        SubjectClass = subjectClass,
                        RollNumber = rollNumber.Trim(),
                        FullName = fullName?.Trim() ?? string.Empty,
                        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
                    };

                    studentLookup[studentKey] = student;
                    result.Students.Add(student);
                }

                foreach (var markMapping in mapping.MarkColumns)
                {
                    if (string.IsNullOrWhiteSpace(markMapping.ComponentName) ||
                        string.IsNullOrWhiteSpace(markMapping.ColumnName))
                    {
                        continue;
                    }

                    var rawValue = GetColumnValue(row, markMapping.ColumnName);
                    if (string.IsNullOrWhiteSpace(rawValue))
                        continue;

                    if (!TryParseMark(rawValue, out var markValue))
                        continue;

                    var componentKey =
                        $"{subjectClassKey}|{markMapping.ComponentName}";

                    if (!componentLookup.TryGetValue(componentKey, out var component))
                        continue;

                    result.Marks.Add(new Mark
                    {
                        Student = student,
                        Component = component,
                        Value = markValue,
                        Comment = null
                    });
                }
            }

            return result;
        }

        public ExcelPreviewRowDto MapPreviewRow(
            ExcelRowData row,
            ExcelColumnMappingDto mapping)
        {
            var preview = new ExcelPreviewRowDto
            {
                RowNumber = row.RowNumber,
                SubjectCode = ResolveValue(
                    mapping.SubjectCode,
                    mapping.SubjectCodeColumn,
                    row) ?? string.Empty,
                ClassName = ResolveValue(
                    mapping.ClassName,
                    mapping.ClassNameColumn,
                    row) ?? string.Empty,
                RollNumber = GetColumnValue(row, mapping.RollNumberColumn),
                FullName = GetColumnValue(row, mapping.FullNameColumn),
                Comment = string.IsNullOrWhiteSpace(mapping.CommentColumn)
                    ? null
                    : GetColumnValue(row, mapping.CommentColumn)
            };

            if (string.IsNullOrWhiteSpace(preview.SubjectCode))
                preview.Errors.Add("Subject code is required.");

            if (string.IsNullOrWhiteSpace(preview.ClassName))
                preview.Errors.Add("Class name is required.");

            if (string.IsNullOrWhiteSpace(preview.RollNumber))
                preview.Errors.Add("Roll number is required.");

            if (string.IsNullOrWhiteSpace(preview.FullName))
                preview.Errors.Add("Full name is required.");

            foreach (var markMapping in mapping.MarkColumns)
            {
                if (string.IsNullOrWhiteSpace(markMapping.ComponentName) ||
                    string.IsNullOrWhiteSpace(markMapping.ColumnName))
                {
                    continue;
                }

                var rawValue = GetColumnValue(row, markMapping.ColumnName);
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    preview.Marks[markMapping.ComponentName] = null;
                    continue;
                }

                if (TryParseMark(rawValue, out var markValue))
                {
                    preview.Marks[markMapping.ComponentName] = markValue;
                }
                else
                {
                    preview.Marks[markMapping.ComponentName] = null;
                    preview.Errors.Add(
                        $"Invalid mark for '{markMapping.ComponentName}': '{rawValue}'.");
                }
            }

            preview.IsValid = preview.Errors.Count == 0;
            return preview;
        }

        private static string? ResolveValue(
            string? fixedValue,
            string? columnName,
            ExcelRowData row)
        {
            if (!string.IsNullOrWhiteSpace(fixedValue))
                return fixedValue.Trim();

            if (string.IsNullOrWhiteSpace(columnName))
                return null;

            var value = GetColumnValue(row, columnName);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string GetColumnValue(ExcelRowData row, string columnName)
        {
            if (row.Values.TryGetValue(columnName, out var value))
                return value;

            return string.Empty;
        }

        private static bool TryParseMark(string rawValue, out decimal markValue)
        {
            if (decimal.TryParse(
                    rawValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out markValue))
            {
                return true;
            }

            return decimal.TryParse(
                rawValue,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out markValue);
        }
    }
}
