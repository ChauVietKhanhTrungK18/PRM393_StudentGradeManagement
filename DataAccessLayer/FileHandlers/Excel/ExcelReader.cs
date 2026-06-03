using ClosedXML.Excel;
using System.Globalization;

namespace DataAccessLayer.FileHandlers.Excel
{
    public class ExcelReader : IExcelReader
    {
        /// <summary>
        /// Numeric grade cells must not use GetString() alone — it often returns empty for numbers.
        /// </summary>
        private static string ReadCellAsText(IXLCell cell)
        {
            if (cell.IsEmpty())
                return string.Empty;

            return cell.DataType switch
            {
                XLDataType.Number =>
                    cell.GetDouble().ToString(CultureInfo.InvariantCulture),
                XLDataType.Boolean => cell.GetBoolean().ToString(),
                XLDataType.DateTime =>
                    cell.GetDateTime().ToString(CultureInfo.InvariantCulture),
                _ => cell.GetFormattedString().Trim()
            };
        }
        public Task<ExcelWorkbookInfo> GetWorkbookInfoAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            cancellationToken.ThrowIfCancellationRequested();

            using var workbook = new XLWorkbook(filePath);
            var info = new ExcelWorkbookInfo();

            foreach (var worksheet in workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sheetInfo = new ExcelSheetInfo
                {
                    Name = worksheet.Name
                };

                var headerRow = worksheet.FirstRowUsed();
                if (headerRow == null)
                {
                    info.Sheets.Add(sheetInfo);
                    continue;
                }

                foreach (var cell in headerRow.CellsUsed())
                {
                    var header = ReadCellAsText(cell);
                    if (!string.IsNullOrEmpty(header))
                    {
                        sheetInfo.Headers.Add(header);
                    }
                }

                info.Sheets.Add(sheetInfo);
            }

            return Task.FromResult(info);
        }

        public Task<List<ExcelRowData>> ReadSheetRowsAsync(
            string filePath,
            string sheetName,
            CancellationToken cancellationToken = default,
            int? maxRows = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentException("Sheet name is required.", nameof(sheetName));

            cancellationToken.ThrowIfCancellationRequested();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(sheetName)
                ?? throw new InvalidOperationException(
                    $"Sheet '{sheetName}' was not found in the workbook.");

            var headerRow = worksheet.FirstRowUsed()
                ?? throw new InvalidOperationException(
                    $"Sheet '{sheetName}' has no data.");

            var headerIndex = new Dictionary<int, string>();
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = ReadCellAsText(cell);
                if (!string.IsNullOrEmpty(header))
                {
                    headerIndex[cell.Address.ColumnNumber] = header;
                }
            }

            var rows = new List<ExcelRowData>();
            var dataRows = worksheet.RowsUsed().Skip(1);

            foreach (var row in dataRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (maxRows.HasValue && rows.Count >= maxRows.Value)
                    break;

                var rowData = new ExcelRowData
                {
                    RowNumber = row.RowNumber()
                };

                var hasValue = false;
                foreach (var (columnNumber, header) in headerIndex)
                {
                    var cellValue = ReadCellAsText(row.Cell(columnNumber));
                    rowData.Values[header] = cellValue;

                    if (!string.IsNullOrEmpty(cellValue))
                        hasValue = true;
                }

                if (hasValue)
                    rows.Add(rowData);
            }

            return Task.FromResult(rows);
        }
    }
}
