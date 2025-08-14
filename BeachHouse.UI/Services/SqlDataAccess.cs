using Dapper;
using Microsoft.Data.SqlClient;
using BeachHouse.UI.Models;
using System.Data;
using System.Text;

namespace BeachHouse.UI.Services
{
    public class SqlDataAccess
    {
        private readonly IConfiguration _config;
        private const string MarketIndexTicker = "SPY";

        public SqlDataAccess(IConfiguration config)
        {
            _config = config;
        }

        private string GetConnectionString() => _config.GetConnectionString("DefaultConnection")!;

        public async Task<IEnumerable<T>> LoadData<T, U>(string storedProcedureOrSql, U parameters)
        {
            using IDbConnection connection = new SqlConnection(GetConnectionString());
            var commandType = storedProcedureOrSql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
                ? CommandType.Text
                : CommandType.StoredProcedure;

            return await connection.QueryAsync<T>(storedProcedureOrSql, parameters, commandType: commandType, commandTimeout: 600);
        }

        // NEW METHODS FOR PROJECT COMPOSER
        public async Task<IEnumerable<Strategy>> GetAllStrategiesAsync()
        {
            const string sql = "SELECT StrategyId, StrategyName, Description, PositionSizingStrategy, PositionSizeValue FROM dbo.Strategies ORDER BY StrategyId;";
            using IDbConnection connection = new SqlConnection(GetConnectionString());
            return await connection.QueryAsync<Strategy>(sql, new { });
        }

        public async Task<Strategy?> GetStrategyByIdAsync(int strategyId)
        {
            const string strategySql = "SELECT * FROM dbo.Strategies WHERE StrategyId = @StrategyId;";
            const string rulesSql = "SELECT * FROM dbo.StrategyRules WHERE StrategyId = @StrategyId;";

            using IDbConnection connection = new SqlConnection(GetConnectionString());
            var strategy = await connection.QuerySingleOrDefaultAsync<Strategy>(strategySql, new { StrategyId = strategyId });
            if (strategy != null)
            {
                strategy.Rules = (await connection.QueryAsync<StrategyRule>(rulesSql, new { StrategyId = strategyId })).ToList();
            }
            return strategy;
        }

        public async Task SaveData<T>(string storedProcedure, T parameters)
        {
            using IDbConnection connection = new SqlConnection(GetConnectionString());
            await connection.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<(DateTime MinDate, DateTime MaxDate)> GetDataDateRangeAsync()
        {
            const string sql = "SELECT MIN(PriceDate), MAX(PriceDate) FROM dbo.DailyPriceHistory;";
            using IDbConnection connection = new SqlConnection(GetConnectionString());
            return await connection.QuerySingleAsync<(DateTime, DateTime)>(sql);
        }

        public async Task<IEnumerable<DailyDataPoint>> GetAllDataWithIndicatorsAsync(DateTime startDate, DateTime endDate, bool onlySP500)
        {
            string universeFilter = onlySP500
               ? $"h.Ticker = '{MarketIndexTicker}' OR h.Ticker IN (SELECT Ticker FROM dbo.SP500_Tickers)"
               : "1=1";

            string sql = $@" 
            ;WITH DateFiltered AS (
                SELECT Ticker, PriceDate, ClosePrice
                FROM dbo.DailyPriceHistory h
                WHERE ({universeFilter}) AND h.PriceDate BETWEEN DATEADD(day, -250, @StartDate) AND @EndDate
            )
            SELECT
                Ticker,
                PriceDate,
                ClosePrice,
                AVG(ClosePrice) OVER (PARTITION BY Ticker ORDER BY PriceDate ROWS BETWEEN 49 PRECEDING AND CURRENT ROW) as SMA50,
                AVG(ClosePrice) OVER (PARTITION BY Ticker ORDER BY PriceDate ROWS BETWEEN 199 PRECEDING AND CURRENT ROW) as SMA200
            FROM DateFiltered
            ORDER BY Ticker, PriceDate;";

            using IDbConnection connection = new SqlConnection(GetConnectionString());
            return await connection.QueryAsync<DailyDataPoint>(sql, new { StartDate = startDate, EndDate = endDate }, commandTimeout: 600);
        }

        public async Task<IEnumerable<ScreenerResult>> RunScreenerQueryAsync(DateTime date, decimal? minClose, long? minVolume, string? signal, bool onlySP500)
        {
            string sp500Join = onlySP500
                ? "INNER JOIN dbo.SP500_Tickers sp ON h.Ticker = sp.Ticker"
                : "";

            var sql = new StringBuilder($@";WITH DateFiltered AS (
    SELECT h.Ticker, h.PriceDate, h.ClosePrice, h.Volume
    FROM dbo.DailyPriceHistory h {sp500Join}
    WHERE h.PriceDate <= @Date AND h.PriceDate >= DATEADD(day, -300, @Date)
),
WithAverages AS (
    SELECT 
        Ticker, 
        PriceDate,
        ClosePrice,
        Volume,
        AVG(ClosePrice) OVER (PARTITION BY Ticker ORDER BY PriceDate ROWS BETWEEN 49 PRECEDING AND CURRENT ROW) as SMA50,
        AVG(ClosePrice) OVER (PARTITION BY Ticker ORDER BY PriceDate ROWS BETWEEN 199 PRECEDING AND CURRENT ROW) as SMA200
    FROM DateFiltered
),
WithCrossoverData AS (
    SELECT 
        *,
        LAG(ClosePrice, 1, 0) OVER (PARTITION BY Ticker ORDER BY PriceDate) as PrevClose,
        LAG(SMA50, 1, 0) OVER (PARTITION BY Ticker ORDER BY PriceDate) as PrevSMA50,
        LAG(SMA200, 1, 0) OVER (PARTITION BY Ticker ORDER BY PriceDate) as PrevSMA200
    FROM WithAverages
),
FinalDay AS (
    SELECT *
    FROM WithCrossoverData
    WHERE PriceDate = (SELECT MAX(PriceDate) FROM DateFiltered d WHERE d.Ticker = WithCrossoverData.Ticker)
)
SELECT Ticker, ClosePrice, Volume, SMA50, SMA200 FROM FinalDay WHERE 1=1 ");

            var parameters = new DynamicParameters();
            parameters.Add("Date", date);

            if (minClose.HasValue)
            {
                sql.Append(" AND ClosePrice >= @MinClose");
                parameters.Add("MinClose", minClose.Value);
            }

            if (minVolume.HasValue)
            {
                sql.Append(" AND Volume >= @MinVolume");
                parameters.Add("MinVolume", minVolume.Value);
            }

            switch (signal)
            {
                case "price_above_sma50":
                    sql.Append(" AND ClosePrice > SMA50 AND PrevClose <= PrevSMA50");
                    break;
                case "price_below_sma50":
                    sql.Append(" AND ClosePrice < SMA50 AND PrevClose >= PrevSMA50");
                    break;
                case "golden_cross":
                    sql.Append(" AND SMA50 > SMA200 AND PrevSMA50 <= PrevSMA200");
                    break;
                case "death_cross":
                    sql.Append(" AND SMA50 < SMA200 AND PrevSMA50 >= PrevSMA200");
                    break;
            }

            sql.Append(" ORDER BY Ticker;");

            using IDbConnection connection = new SqlConnection(GetConnectionString());
            return await connection.QueryAsync<ScreenerResult>(sql.ToString(), parameters, commandTimeout: 300);
        }

        public async Task BulkInsertDailyPriceHistory(List<DailyPriceHistory> records)
        {
            var table = new DataTable();
            table.Columns.Add("Ticker", typeof(string));
            table.Columns.Add("PriceDate", typeof(DateTime));
            table.Columns.Add("OpenPrice", typeof(decimal));
            table.Columns.Add("HighPrice", typeof(decimal));
            table.Columns.Add("LowPrice", typeof(decimal));
            table.Columns.Add("ClosePrice", typeof(decimal));
            table.Columns.Add("Volume", typeof(long));

            foreach (var record in records)
            {
                table.Rows.Add(record.Ticker, record.PriceDate, record.OpenPrice, record.HighPrice, record.LowPrice, record.ClosePrice, record.Volume);
            }

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                await connection.OpenAsync();
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "DailyPriceHistory";
                    bulkCopy.ColumnMappings.Add("Ticker", "Ticker");
                    bulkCopy.ColumnMappings.Add("PriceDate", "PriceDate");
                    bulkCopy.ColumnMappings.Add("OpenPrice", "OpenPrice");
                    bulkCopy.ColumnMappings.Add("HighPrice", "HighPrice");
                    bulkCopy.ColumnMappings.Add("LowPrice", "LowPrice");
                    bulkCopy.ColumnMappings.Add("ClosePrice", "ClosePrice");
                    bulkCopy.ColumnMappings.Add("Volume", "Volume");

                    await bulkCopy.WriteToServerAsync(table);
                }
            }
        }
    }
}