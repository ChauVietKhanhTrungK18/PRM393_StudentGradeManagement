using System.Text.RegularExpressions;
using DataAccessLayer.Entities;

namespace BusinessLayer.Mapping
{
    public static class ExcelWorksheetNaming
    {
        private static readonly Regex InvalidChars = new(@"[:\\\/\?\*\[\]]");

        public static string ToWorksheetName(string subjectCode, string className)
        {
            var input = $"{subjectCode}_{className}";
            var name = InvalidChars.Replace(input, "_").Trim();
            if (name.Length > 31)
                name = name[..31];

            return string.IsNullOrEmpty(name) ? "Sheet1" : name;
        }

        public static SubjectClass? MatchSheet(
            string sheetName,
            IEnumerable<SubjectClass> subjectClasses)
        {
            foreach (var sc in subjectClasses)
            {
                if (string.Equals(
                        sheetName,
                        ToWorksheetName(sc.SubjectCode, sc.ClassName),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return sc;
                }

                if (string.Equals(
                        sheetName,
                        $"{sc.SubjectCode}_{sc.ClassName}",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return sc;
                }
            }

            return null;
        }
    }
}
