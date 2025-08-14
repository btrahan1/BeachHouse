namespace BeachHouse.UI.Services
{
    public interface IDataIngestionService
    {
        Task IngestStockDataFromZipAsync(Stream zipStream, Action<string> onProgress);
        Task IngestSpyDataAsync(Stream csvStream, Action<string> onProgress);
    }
}