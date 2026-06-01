using Microsoft.AspNetCore.Http;

namespace BusinessLayer.DTOs
{
    public class ExcelReadRequestDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
