using BeachHouse.UI.Models;

namespace BeachHouse.UI.Services
{
    public class MarketDataService : IMarketDataService
    {
        private readonly SqlDataAccess _db;

        public MarketDataService(SqlDataAccess db)
        {
            _db = db;
        }

        // NEW METHOD for Project Afterburner
        public Task<IEnumerable<DailyDataPoint>> GetAllDataWithIndicatorsAsync(DateTime startDate, DateTime endDate, bool onlySP500)
        {
            return _db.GetAllDataWithIndicatorsAsync(startDate, endDate, onlySP500);
        }

        // This method is now legacy for the backtester, but still used by other parts of the app (like modals)
        public async Task<DailyPriceHistory?> GetQuoteForDateAsync(string ticker, DateTime date)
        {
            const string sql = @"
                SELECT TOP 1 Ticker, PriceDate, OpenPrice, HighPrice, LowPrice, ClosePrice, Volume
                FROM dbo.DailyPriceHistory
                WHERE Ticker = @Ticker AND PriceDate <= @Date
                ORDER BY PriceDate DESC; ";

            var result = await _db.LoadData<DailyPriceHistory, dynamic>(sql, new { Ticker = ticker, Date = date });
            return result.FirstOrDefault();
        }

        // This method is now legacy for the backtester, but still used by the Screener page
        public async Task<IEnumerable<ScreenerResult>> RunScreenerAsync(DateTime date, decimal? minClose, long? minVolume, string? signal, bool onlySP500)
        {
            return await _db.RunScreenerQueryAsync(date, minClose, minVolume, signal, onlySP500);
        }
    }
}