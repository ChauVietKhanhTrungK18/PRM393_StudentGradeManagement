using Microsoft.AspNetCore.Http;

namespace BusinessLayer.DTOs
{
    public class ExcelPreviewRequestDto
    {
        public IFormFile File { get; set; } = null!;

        public string MappingJson { get; set; } = string.Empty;
    }
}
