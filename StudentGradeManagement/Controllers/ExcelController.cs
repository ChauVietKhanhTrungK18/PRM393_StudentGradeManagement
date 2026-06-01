#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessLayer.DTOs;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentGradeManagement.DTOs;
using System.Text.Json;

namespace StudentGradeManagement.Controllers
{
    [ApiController]
    [Route("api/excel")]
    public class ExcelController : ControllerBase
    {
        private readonly IExcelService _excelService;
        private readonly IExcelImportService _importService;
        private readonly ILogger<ExcelController> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ExcelController(IExcelService excelService, ILogger<ExcelController> logger, IExcelImportService importService){
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Read an Excel workbook and return sheet names with column headers.
        /// </summary>
        [HttpPost("read")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ExcelReadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExcelReadResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Read(
            [FromForm] ExcelReadRequestDto request,
            CancellationToken cancellationToken)
        {
            var validation = ValidateExcelFile(request.File);
            if (validation != null)
                return validation;

            var tempFilePath = CreateTempPath(".xlsx");

            try
            {
                await SaveTempFileAsync(request.File!, tempFilePath, cancellationToken);

                var result =
                    await _importService.ReadWorkbookAsync(
                        tempFilePath,
                        cancellationToken);

                return Ok(new ExcelReadResponseDto
                {
                    Success = true,
                    Message = "Workbook read successfully.",
                    Sheets = result.Sheets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading Excel file.");
                return BadRequest(new ExcelReadResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            finally
            {
                DeleteTempFile(tempFilePath);
            }
        }

        /// <summary>
        /// Preview mapped Excel rows before import.
        /// </summary>
        [HttpPost("preview")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ExcelPreviewResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExcelPreviewResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Preview(
            [FromForm] ExcelPreviewRequestDto request,
            CancellationToken cancellationToken)
        {
            var validation = ValidateExcelFile(request.File);
            if (validation != null)
                return validation;

            if (!TryParseMapping(request.MappingJson, out var mapping, out var parseError))
            {
                return BadRequest(new ExcelPreviewResponseDto
        {
                    Success = false,
                    Message = parseError
                });
            }

            var tempFilePath = CreateTempPath(".xlsx");

            try
            {
                await SaveTempFileAsync(request.File, tempFilePath, cancellationToken);

                var result =
                    await _importService.PreviewAsync(
                        tempFilePath,
                        mapping!,
                        cancellationToken);

                return Ok(new ExcelPreviewResponseDto
                {
                    Success = true,
                    Message = "Preview generated successfully.",
                    TotalRows = result.TotalRows,
                    ValidRows = result.ValidRows,
                    InvalidRows = result.InvalidRows,
                    Rows = result.Rows,
                    Warnings = result.Warnings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing Excel file.");
                return BadRequest(new ExcelPreviewResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            finally
            {
                DeleteTempFile(tempFilePath);
            }
        }

        /// <summary>
        /// Import mapped Excel data into SQLite (overwrite existing marks).
        /// </summary>
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ExcelImportResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExcelImportResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ExcelImportResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Import(
            [FromForm] ExcelImportRequestDto request,
            CancellationToken cancellationToken)
        {
            var validation = ValidateExcelFile(request.File);
            if (validation != null)
                return validation;

            if (!TryParseMapping(request.MappingJson, out var mapping, out var parseError))
            {
                return BadRequest(new ExcelImportResponseDto
                {
                    Success = false,
                    Message = parseError
                });
            }

            var tempFilePath = CreateTempPath(".xlsx");

            try
            {
                await SaveTempFileAsync(request.File, tempFilePath, cancellationToken);

                _logger.LogInformation(
                    "Starting Excel import for sheet {Sheet}",
                    mapping!.SheetName);

                var result =
                    await _importService.ImportAsync(
                        tempFilePath,
                        mapping,
                        string.IsNullOrWhiteSpace(request.ChangedBy)
                            ? "excel-import"
                            : request.ChangedBy,
                        cancellationToken);

                return Ok(new ExcelImportResponseDto
                {
                    Success = true,
                    Message = "Import successful.",
                    SubjectClassCount = result.SubjectClassCount,
                    StudentCount = result.StudentCount,
                    ComponentCount = result.ComponentCount,
                    MarkCount = result.MarkCount,
                    SkippedRows = result.SkippedRows,
                    ImportLog = result.ImportLog
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Excel file.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ExcelImportResponseDto
                    {
                        Success = false,
                        Message = ex.Message
                    });
            }
            finally
            {
                DeleteTempFile(tempFilePath);
            }
        }

        private BadRequestObjectResult? ValidateExcelFile(IFormFile? file)
        {
            if (file == null)
            {
                return BadRequest(new ExcelReadResponseDto
                {
                    Success = false,
                    Message = "No file provided."
                });
            }

            if (file.Length == 0)
            {
                return BadRequest(new ExcelReadResponseDto
                {
                    Success = false,
                    Message = "File is empty."
                });
            }

            if (!string.Equals(
                    Path.GetExtension(file.FileName),
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ExcelReadResponseDto
                {
                    Success = false,
                    Message = "Only .xlsx files are allowed."
                });
            }

            return null;
        }

        private static bool TryParseMapping(
            string? mappingJson,
            out ExcelColumnMappingDto? mapping,
            out string error)
        {
            mapping = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(mappingJson))
            {
                error = "Column mapping JSON is required.";
                return false;
            }

            try
            {
                mapping = JsonSerializer.Deserialize<ExcelColumnMappingDto>(
                    mappingJson,
                    JsonOptions);

                if (mapping == null)
                {
                    error = "Column mapping JSON is invalid.";
                    return false;
                }

                return true;
            }
            catch (JsonException ex)
            {
                error = $"Invalid mapping JSON: {ex.Message}";
                return false;
            }
        }

        private static string CreateTempPath(string extension) =>
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");

        private static async Task SaveTempFileAsync(
            IFormFile file,
            string tempFilePath,
            CancellationToken cancellationToken)
        {
            await using var stream = System.IO.File.Create(tempFilePath);
            await file.CopyToAsync(stream, cancellationToken);
        }

        private static void DeleteTempFile(string tempFilePath)
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
        /// <summary>
        /// Export Excel template with one worksheet per SubjectClass.
        /// </summary>
        [HttpGet("template")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportTemplate(CancellationToken cancellationToken)
        {
            try
            {
                var fileBytes = await _excelService.ExportTemplateAsync(cancellationToken).ConfigureAwait(false);
                const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                const string fileName = "GradeTemplate.xlsx";
                return File(fileBytes, contentType, fileName);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("ExportTemplate cancelled by caller.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Export cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export Excel template.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to export template.");
            }
        }
    }
}
