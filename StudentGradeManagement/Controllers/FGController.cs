using BusinessLayer.DTOs;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentGradeManagement.DTOs;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StudentGradeManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FGController : ControllerBase
    {
        private readonly IFGImportService _importService;
        private readonly ILogger<FGController> _logger;

        public FGController(
            IFGImportService importService,
            ILogger<FGController> logger)
        {
            _importService = importService
                ?? throw new ArgumentNullException(nameof(importService));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Import FG file.
        /// </summary>
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(FGImportResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FGImportResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FGImportResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Import(
            [FromForm] FGImportRequestDto request,
            CancellationToken cancellationToken)
        {
            var file = request.File;
            if (file == null)
            {
                return BadRequest(new FGImportResponseDto
                {
                    Success = false,
                    Message = "No file provided."
                });
            }

            if (file.Length == 0)
            {
                return BadRequest(new FGImportResponseDto
                {
                    Success = false,
                    Message = "File is empty."
                });
            }

            if (!string.Equals(
                    Path.GetExtension(file.FileName),
                    ".fg",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new FGImportResponseDto
                {
                    Success = false,
                    Message = "Only .fg files are allowed."
                });
            }

            var tempFilePath =
                Path.Combine(
                    Path.GetTempPath(),
                    $"{Guid.NewGuid():N}.fg");

            try
            {
                await using (var stream = System.IO.File.Create(tempFilePath))
                {
                    await file.CopyToAsync(
                        stream,
                        cancellationToken);
                }

                _logger.LogInformation(
                    "Starting FG import: {File}",
                    file.FileName);

                FGImportResultDto result =
                    await _importService
                        .ImportFromFileAsync(
                            tempFilePath,
                            cancellationToken);

                return Ok(new FGImportResponseDto
                {
                    Success = true,
                    SubjectClassCount = result.SubjectClassCount,
                    StudentCount = result.StudentCount,
                    ComponentCount = result.ComponentCount,
                    MarkCount = result.MarkCount,
                    Message = "Import successful."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error importing FG file.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new FGImportResponseDto
                    {
                        Success = false,
                        Message = ex.Message
                    });
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }
    }
}