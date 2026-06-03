namespace BusinessLayer.DTOs
{
    public class ExcelUploadResultDto
    {
        public string FilePath { get; set; } = string.Empty;

        public List<ExcelSheetInfoDto> Sheets { get; set; } = new();
    }

    public class ExcelSheetInfoDto
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Headers { get; set; } = new();
    }
}
