namespace BusinessLayer.DTOs
{
    public class ExcelReadResultDto
    {
        public List<ExcelSheetInfoDto> Sheets { get; set; } = new();
    }

    public class ExcelSheetInfoDto
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Headers { get; set; } = new();
    }
}
