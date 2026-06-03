using BusinessLayer.DTOs;

namespace BusinessLayer.Mapping
{
    public static class ExcelAutoMappingBuilder
    {
        private static readonly string[] RollCandidates = { "Roll", "MSSV", "RollNumber" };
        private static readonly string[] NameCandidates = { "Name", "FullName", "Họ tên", "Ho ten" };
        private static readonly string[] CommentCandidates = { "Comment", "Ghi chú", "Ghi chu" };

        public static ExcelColumnMappingDto Build(
            IReadOnlyList<string> headers,
            IEnumerable<string> componentNames)
        {
            var rollColumn = FindHeader(headers, RollCandidates)
                ?? throw new InvalidOperationException(
                    "Sheet must have a Roll (or MSSV) column.");

            var nameColumn = FindHeader(headers, NameCandidates) ?? "Name";
            var commentColumn = FindHeader(headers, CommentCandidates);

            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                rollColumn,
                nameColumn
            };

            if (!string.IsNullOrEmpty(commentColumn))
                reserved.Add(commentColumn);

            var componentLookup = componentNames
                .ToDictionary(c => c, StringComparer.OrdinalIgnoreCase);

            var marks = new List<ExcelMarkMappingDto>();

            foreach (var header in headers)
            {
                if (reserved.Contains(header))
                    continue;

                if (!componentLookup.TryGetValue(header, out var componentName))
                    continue;

                marks.Add(new ExcelMarkMappingDto
                {
                    ExcelColumn = header,
                    ComponentName = componentName
                });
            }

            if (marks.Count == 0)
            {
                throw new InvalidOperationException(
                    "No grade columns matched grading components in the database.");
            }

            return new ExcelColumnMappingDto
            {
                RollNumberColumn = rollColumn,
                FullNameColumn = nameColumn,
                CommentColumn = commentColumn,
                Marks = marks
            };
        }

        private static string? FindHeader(
            IReadOnlyList<string> headers,
            string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var match = headers.FirstOrDefault(
                    h => h.Equals(candidate, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
