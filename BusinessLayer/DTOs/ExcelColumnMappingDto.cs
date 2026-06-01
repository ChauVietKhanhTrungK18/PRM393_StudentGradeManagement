namespace BusinessLayer.DTOs
{
    public class ExcelColumnMappingDto
    {
        public string SheetName { get; set; } = string.Empty;

        /// <summary>Fixed subject code when not mapped from a column.</summary>
        public string? SubjectCode { get; set; }

        public string? SubjectCodeColumn { get; set; }

        /// <summary>Fixed class name when not mapped from a column.</summary>
        public string? ClassName { get; set; }

        public string? ClassNameColumn { get; set; }

        public string RollNumberColumn { get; set; } = string.Empty;

        public string FullNameColumn { get; set; } = string.Empty;

        public string? CommentColumn { get; set; }

        public List<ExcelMarkColumnMappingDto> MarkColumns { get; set; } = new();
    }

    public class ExcelMarkColumnMappingDto
    {
        public string ComponentName { get; set; } = string.Empty;

        public string ColumnName { get; set; } = string.Empty;
    }
}
