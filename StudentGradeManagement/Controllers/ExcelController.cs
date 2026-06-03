#nullable enable

using BusinessLayer.DTOs;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.DTOs;

namespace StudentGradeManagement.Controllers
{
    [ApiController]
    [Route("api/excel")]
    public class ExcelController : ControllerBase
    {
        private readonly IExcelImportService _importService;
        private readonly IExcelService _excelService;
        private readonly ILogger<ExcelController> _logger;

        public ExcelController(
            IExcelImportService importService,
            IExcelService excelService,
            ILogger<ExcelController> logger)
        {
            _importService = importService
                ?? throw new ArgumentNullException(nameof(importService));
            _excelService = excelService
                ?? throw new ArgumentNullException(nameof(excelService));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// API 7 — Upload and read workbook structure (sheets + headers).
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ExcelUploadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload(
            [FromForm] ExcelUploadRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result =
                    await _importService.UploadAsync(request.File, cancellationToken);

                return Ok(new ExcelUploadResponseDto
                {
                    FilePath = result.FilePath,
                    Sheets = result.Sheets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel upload failed.");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// [Import all sheet - use file path from API7] Auto-sync all sheets from a previously uploaded file (after API 7)
        /// </summary>
        [HttpPost("sync")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ExcelSyncResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Sync(
            [FromBody] ExcelSyncRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result =
                    await _importService.SyncAllSheetsAsync(request, cancellationToken);

                return Ok(MapSyncResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel sync failed.");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// API 8 — Preview grade changes (current vs new) before import.[Preview 1 sheet]
        /// </summary>
        [HttpPost("preview")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ExcelPreviewResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Preview(
            [FromBody] ExcelPreviewRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result =
                    await _importService.PreviewAsync(request, cancellationToken);

                return Ok(new ExcelPreviewResponseDto
                {
                    Preview = result.Preview,
                    NotFoundRolls = result.NotFoundRolls,
                    TotalRows = result.TotalRows,
                    ValidRows = result.ValidRows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel preview failed.");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// API 9 — Confirm and write grades to SQLite.[Import 1 sheet]
        /// </summary>
        [HttpPost("import")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ExcelImportResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExcelImportResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Import(
            [FromBody] ExcelImportRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Excel import for {Subject} {Class}, sheet {Sheet}",
                    request.SubjectCode,
                    request.ClassName,
                    request.SheetName);

                var result =
                    await _importService.ImportAsync(request, cancellationToken);

                if (!result.Success)
                    return BadRequest(MapImportResponse(result));

                return Ok(MapImportResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel import failed.");
                return BadRequest(new ExcelImportResponseDto
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                });
            }
        }
        /// <summary>
        /// Upload Excel and auto-sync all sheets (match sheet name → class, map columns, import changes).[Import all sheet]
        /// </summary>
        [HttpPost("upload-sync")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ExcelSyncResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadAndSync(
            [FromForm] ExcelUploadRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result =
                    await _importService.UploadAndSyncAsync(request.File, cancellationToken);

                return Ok(MapSyncResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel upload-sync failed.");
                return BadRequest(new { message = ex.Message });
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
                var fileBytes =
                    await _excelService.ExportTemplateAsync(cancellationToken)
                        .ConfigureAwait(false);

                const string contentType =
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(fileBytes, contentType, "GradeTemplate.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export Excel template.");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Failed to export template." });
            }
        }

        private static ExcelImportResponseDto MapImportResponse(
            BusinessLayer.DTOs.ExcelImportResultDto result) =>
            new()
            {
                Success = result.Success,
                ImportedCount = result.ImportedCount,
                SkippedCount = result.SkippedCount,
                NotFoundRolls = result.NotFoundRolls,
                Errors = result.Errors
            };

        private static ExcelSyncResponseDto MapSyncResponse(
            BusinessLayer.DTOs.ExcelSyncResultDto result) =>
            new()
            {
                FilePath = result.FilePath,
                Success = result.Success,
                TotalSheets = result.TotalSheets,
                ImportedSheets = result.ImportedSheets,
                SkippedSheets = result.SkippedSheets,
                Sheets = result.Sheets.Select(s => new ExcelSheetSyncResponseItemDto
                {
                    SheetName = s.SheetName,
                    SubjectCode = s.SubjectCode,
                    ClassName = s.ClassName,
                    Status = s.Status,
                    Message = s.Message,
                    UpdatedStudents = s.UpdatedStudents,
                    UpdatedMarks = s.UpdatedMarks,
                    ChangedMarksPreview = s.ChangedMarksPreview,
                    NotFoundRolls = s.NotFoundRolls
                }).ToList()
            };
    }
}
