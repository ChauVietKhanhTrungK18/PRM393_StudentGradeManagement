namespace DataAccessLayer.FileHandlers.Excel
{
    public interface IExcelUploadStore
    {
        Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default);

        string GetPhysicalPath(string relativeFilePath);

        bool Exists(string relativeFilePath);
    }
}
