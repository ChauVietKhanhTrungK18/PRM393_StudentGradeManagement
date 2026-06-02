using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

#nullable enable

namespace StudentGradeManagement.Controllers
{
    [ApiController]
    [Route("api/fg")]
    public class FGExportController : ControllerBase
    {
        private readonly IFGExportService _exportService;
        private readonly ILogger<FGExportController> _logger;

        public FGExportController(IFGExportService exportService, ILogger<FGExportController> logger)
        {
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Export current DB state to an .fg file compatible with the import pipeline.
        /// </summary>
        [HttpPost("export")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            try
            {
                var bytes = await _exportService.ExportAsync(cancellationToken).ConfigureAwait(false);
                const string contentType = "application/octet-stream";
                const string fileName = "export.fg";
                return File(bytes, contentType, fileName);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("FG export cancelled.");
                return StatusCode(500, "Export cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FG export failed.");
                return StatusCode(500, "Export failed: " + ex.Message);
            }
        }
    }
}
