#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace StudentGradeManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExcelController : ControllerBase
    {
        private readonly IExcelService _excelService;
        private readonly ILogger<ExcelController> _logger;

        public ExcelController(IExcelService excelService, ILogger<ExcelController> logger)
        {
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
