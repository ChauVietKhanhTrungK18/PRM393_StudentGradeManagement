using Microsoft.AspNetCore.Http;

namespace BusinessLayer.DTOs
{
    public class ExcelImportRequestDto
    {
        public IFormFile File { get; set; } = null!;

        public string MappingJson { get; set; } = string.Empty;

        public string? ChangedBy { get; set; }
    }
}
