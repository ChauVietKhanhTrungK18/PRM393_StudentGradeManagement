namespace DataAccessLayer.FileHandlers.Excel
{
    public class ExcelUploadStore : IExcelUploadStore
    {
        private readonly string _rootDirectory;
        private const string TempFolderName = "temp";

        public ExcelUploadStore(string contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
                throw new ArgumentNullException(nameof(contentRootPath));

            _rootDirectory = Path.Combine(contentRootPath, TempFolderName);
            Directory.CreateDirectory(_rootDirectory);
        }

        public async Task<string> SaveAsync(
            Stream content,
            CancellationToken cancellationToken = default)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var fileName = $"{Guid.NewGuid():N}.xlsx";
            var physicalPath = Path.Combine(_rootDirectory, fileName);

            await using var fileStream = File.Create(physicalPath);
            await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

            return $"{TempFolderName}/{fileName}";
        }

        public string GetPhysicalPath(string relativeFilePath)
        {
            if (string.IsNullOrWhiteSpace(relativeFilePath))
                throw new ArgumentException("File path is required.", nameof(relativeFilePath));

            var normalized = relativeFilePath
                .Replace('\\', '/')
                .TrimStart('/');

            if (!normalized.StartsWith($"{TempFolderName}/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid upload file path.");

            var fileName = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(fileName) || fileName.Contains(".."))
                throw new InvalidOperationException("Invalid upload file path.");

            return Path.Combine(_rootDirectory, fileName);
        }

        public bool Exists(string relativeFilePath)
        {
            try
            {
                var physicalPath = GetPhysicalPath(relativeFilePath);
                return File.Exists(physicalPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
