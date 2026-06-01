using Microsoft.AspNetCore.Http;

namespace BusinessLayer.DTOs
{
    public class ExcelUploadRequestDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
