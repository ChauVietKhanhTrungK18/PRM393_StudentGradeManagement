using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BusinessLayer.IService;
using DataAccessLayer.IRepository;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace BusinessLayer.Services
{
    public class ExcelService : IExcelService
    {
        private readonly IExcelRepository _excelRepository;
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(IExcelRepository excelRepository, ILogger<ExcelService> logger)
        {
            _excelRepository = excelRepository ?? throw new ArgumentNullException(nameof(excelRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<byte[]> ExportTemplateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var subjectClasses = await _excelRepository.GetAllForExportAsync(cancellationToken).ConfigureAwait(false);

                await using var ms = new MemoryStream();
                using var package = new ExcelPackage();

                foreach (var sc in subjectClasses)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var components = sc.GradingComponents.OrderBy(c => c.Name).ToArray();

                    var sheetName = MakeValidWorksheetName($"{sc.SubjectCode}_{sc.ClassName}");
                    var ws = package.Workbook.Worksheets.Add(sheetName);

                    // Header row
                    ws.Cells[1, 1].Value = "Roll";
                    ws.Cells[1, 2].Value = "Name";
                    ws.Cells[1, 3].Value = "Comment";

                    for (var i = 0; i < components.Length; i++)
                    {
                        ws.Cells[1, 4 + i].Value = components[i].Name;
                    }

                    // Bold header
                    using (var hdr = ws.Cells[1, 1, 1, 3 + components.Length])
                    {
                        hdr.Style.Font.Bold = true;
                    }

                    // Rows: students
                    var students = (sc.Students ?? Array.Empty<DataAccessLayer.Entities.Student>())
                        .OrderBy(s => s.RollNumber)
                        .ToArray();

                    var row = 2;
                    foreach (var st in students)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        ws.Cells[row, 1].Value = st.RollNumber;
                        ws.Cells[row, 2].Value = st.FullName;
                        ws.Cells[row, 3].Value = st.Comment;

                        for (var ci = 0; ci < components.Length; ci++)
                        {
                            var comp = components[ci];
                            var mark = st.Marks?.FirstOrDefault(m => m.ComponentId == comp.Id);
                            // Mark entity may use ComponentId or GradingComponentId depending on model; check both.
                            if (mark != null)
                            {
                                // mark.Value type depends on your entity (decimal/float). Convert safely.
                                var doubleValue = Convert.ToDouble(mark.Value);
                                ws.Cells[row, 4 + ci].Value = doubleValue;
                                ws.Cells[row, 4 + ci].Style.Numberformat.Format = "0.###";
                                
                            }
                            else
                            {
                                ws.Cells[row, 4 + ci].Value = null;
                            }
                        }

                        row++;
                    }

                    // Autofit columns
                    var lastCol = 3 + components.Length;
                    var lastRow = Math.Max(2, row - 1);
                    ws.Cells[1, 1, lastRow, lastCol].AutoFitColumns();
                }

                package.SaveAs(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel template export failed.");
                throw;
            }
        }

        private static string MakeValidWorksheetName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "Sheet1";
            // Remove invalid characters : \ / ? * [ ]
            var invalid = new Regex(@"[:\\\/\?\*\[\]]");
            var name = invalid.Replace(input, "_").Trim();
            if (name.Length > 31) name = name.Substring(0, 31);
            return string.IsNullOrEmpty(name) ? "Sheet1" : name;
        }
    }
}
