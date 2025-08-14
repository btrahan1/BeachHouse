using BeachHouse.UI.Models;

namespace BeachHouse.UI.Services
{
    public interface IMarketDataService
    {
        Task<DailyPriceHistory?> GetQuoteForDateAsync(string ticker, DateTime date);
        Task<IEnumerable<ScreenerResult>> RunScreenerAsync(DateTime date, decimal? minClose, long? minVolume, string? signal, bool onlySP500);
        Task<IEnumerable<DailyDataPoint>> GetAllDataWithIndicatorsAsync(DateTime startDate, DateTime endDate, bool onlySP500);
    }
}