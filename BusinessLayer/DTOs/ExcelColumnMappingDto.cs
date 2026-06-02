namespace BusinessLayer.DTOs
{
    public class ExcelColumnMappingDto
    {
        public string RollNumberColumn { get; set; } = "Roll";

        public string FullNameColumn { get; set; } = "Name";

        public string? CommentColumn { get; set; }

        public List<ExcelMarkMappingDto> Marks { get; set; } = new();
    }

    public class ExcelMarkMappingDto
    {
        public string ExcelColumn { get; set; } = string.Empty;

        public string ComponentName { get; set; } = string.Empty;
    }
}
