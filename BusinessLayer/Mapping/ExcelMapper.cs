#nullable enable

using BusinessLayer.DTOs;
using DataAccessLayer.FileHandlers.Excel;
using System.Globalization;

namespace BusinessLayer.Mapping
{
    public class ExcelMapper
    {
        public ParsedExcelRow? ParseRow(
            ExcelRowData row,
            ExcelColumnMappingDto mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            var rollNumber = GetColumnValue(row, mapping.RollNumberColumn);
            if (string.IsNullOrWhiteSpace(rollNumber))
                return null;

            var parsed = new ParsedExcelRow
            {
                RowNumber = row.RowNumber,
                RollNumber = rollNumber.Trim(),
                FullName = GetColumnValue(row, mapping.FullNameColumn),
                Comment = string.IsNullOrWhiteSpace(mapping.CommentColumn)
                    ? null
                    : GetColumnValue(row, mapping.CommentColumn)
            };

            foreach (var markMapping in mapping.Marks)
            {
                if (string.IsNullOrWhiteSpace(markMapping.ExcelColumn) ||
                    string.IsNullOrWhiteSpace(markMapping.ComponentName))
                {
                    continue;
                }

                var rawValue = GetColumnValue(row, markMapping.ExcelColumn);
                if (string.IsNullOrWhiteSpace(rawValue))
                    continue;

                if (!TryParseMark(rawValue, out var markValue))
                {
                    parsed.InvalidMarks.Add(
                        $"{markMapping.ComponentName}: invalid value '{rawValue}'");
                    continue;
                }

                parsed.Marks[markMapping.ComponentName.Trim()] = markValue;
            }

            return parsed;
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

    public class ParsedExcelRow
    {
        public int RowNumber { get; set; }

        public string RollNumber { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Comment { get; set; }

        public Dictionary<string, decimal> Marks { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public List<string> InvalidMarks { get; } = new();
    }
}
