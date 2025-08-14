using BeachHouse.UI.Models;
using System.Diagnostics;

namespace BeachHouse.UI.Services
{
    public class BacktestService : IBacktestService
    {
        private readonly IMarketDataService _marketData;
        private readonly SqlDataAccess _db;
        private const string MarketIndexTicker = "SPY";

        public BacktestService(IMarketDataService marketData, SqlDataAccess db)
        {
            _marketData = marketData;
            _db = db;
        }

        public Task<IEnumerable<Strategy>> GetAllStrategiesAsync()
        {
            return _db.GetAllStrategiesAsync();
        }

        public async Task<BacktestResult> RunBacktestAsync(BacktestParameters parameters, bool onlySP500, Action<string> onProgress)
        {
            var stopwatch = Stopwatch.StartNew();
            onProgress("Initializing backtest...");

            // STAGE 0: LOAD STRATEGY DEFINITION
            onProgress($"Loading strategy definition for ID: {parameters.StrategyId}...");
            var strategy = await _db.GetStrategyByIdAsync(parameters.StrategyId);
            if (strategy == null)
            {
                onProgress($"FATAL ERROR: Strategy with ID {parameters.StrategyId} not found.");
                return new BacktestResult();
            }
            var entryRules = new HashSet<string>(strategy.Rules.Where(r => r.RuleType == "Entry").Select(r => r.SignalName));
            var exitRules = new HashSet<string>(strategy.Rules.Where(r => r.RuleType == "Exit").Select(r => r.SignalName));
            onProgress($"Strategy '{strategy.StrategyName}' loaded successfully.");

            // STAGE 1: UPFRONT DATA LOAD
            onProgress("Loading and pre-calculating all historical data and indicators...");
            var allData = await _marketData.GetAllDataWithIndicatorsAsync(parameters.StartDate, parameters.EndDate, onlySP500);
            onProgress($"Data loaded in {stopwatch.Elapsed.TotalSeconds:N2} seconds. Structuring data for high-speed lookup...");

            var marketDataLookup = allData.GroupBy(d => d.Ticker).ToDictionary(g => g.Key, g => g.OrderBy(p => p.PriceDate).ToList());
            if (!marketDataLookup.ContainsKey(MarketIndexTicker))
            {
                onProgress($"FATAL ERROR: Market Index Ticker '{MarketIndexTicker}' not found in the dataset.");
                return new BacktestResult();
            }
            var spyData = marketDataLookup[MarketIndexTicker].ToDictionary(d => d.PriceDate, d => d);
            var allTradableTickers = marketDataLookup.Keys.Where(k => k != MarketIndexTicker).ToList();
            var tradingDays = allData.Select(d => d.PriceDate).Distinct().OrderBy(d => d).ToList();
            onProgress("Data structured. Starting simulation...");

            // STAGE 2: IN-MEMORY SIMULATION
            var result = new BacktestResult { InitialCapital = parameters.InitialCapital };
            var virtualPortfolio = new List<SimulatedTrade>();
            var availableCapital = parameters.InitialCapital;

            foreach (var currentDate in tradingDays)
            {
                if (currentDate < parameters.StartDate || currentDate > parameters.EndDate) continue;

                // 1. Process Exits
                foreach (var trade in virtualPortfolio.Where(t => t.IsOpen).ToList())
                {
                    if (!marketDataLookup.ContainsKey(trade.Ticker)) continue;
                    var dayIndex = marketDataLookup[trade.Ticker].FindIndex(d => d.PriceDate == currentDate);
                    if (dayIndex < 1) continue;

                    var todayData = marketDataLookup[trade.Ticker][dayIndex];
                    var yesterdayData = marketDataLookup[trade.Ticker][dayIndex - 1];
                    bool shouldExit = false;

                    if (exitRules.Contains("DeathCross"))
                    {
                        if (todayData.SMA50.HasValue && todayData.SMA200.HasValue && yesterdayData.SMA50.HasValue && yesterdayData.SMA200.HasValue &&
                            todayData.SMA50 < todayData.SMA200 && yesterdayData.SMA50 >= yesterdayData.SMA200)
                        {
                            shouldExit = true;
                        }
                    }

                    if (shouldExit)
                    {
                        trade.ExitDate = currentDate;
                        trade.ExitPrice = todayData.ClosePrice;
                        availableCapital += (decimal)(trade.ExitPrice * trade.Shares);
                    }
                }

                // 2. Process Entries
                bool canEnter = true;
                if (entryRules.Contains("RegimeFilter"))
                {
                    if (!spyData.TryGetValue(currentDate, out var todaySpy) || !todaySpy.SMA200.HasValue || todaySpy.ClosePrice <= todaySpy.SMA200.Value)
                    {
                        canEnter = false;
                    }
                }
                if (entryRules.Contains("Q2Filter") && (currentDate.Month >= 4 && currentDate.Month <= 6))
                {
                    canEnter = false;
                }

                if (canEnter)
                {
                    foreach (var ticker in allTradableTickers)
                    {
                        decimal amountToInvest = CalculateAmountToInvest(strategy, availableCapital, virtualPortfolio, marketDataLookup, currentDate);

                        if (availableCapital < amountToInvest) continue;
                        if (virtualPortfolio.Any(t => t.Ticker == ticker && t.IsOpen)) continue;

                        var dayIndex = marketDataLookup.ContainsKey(ticker) ? marketDataLookup[ticker].FindIndex(d => d.PriceDate == currentDate) : -1;
                        if (dayIndex < 1) continue;

                        var todayData = marketDataLookup[ticker][dayIndex];
                        var yesterdayData = marketDataLookup[ticker][dayIndex - 1];
                        bool signalFired = false;

                        if (entryRules.Contains("GoldenCross"))
                        {
                            if (todayData.SMA50.HasValue && todayData.SMA200.HasValue && yesterdayData.SMA50.HasValue && yesterdayData.SMA200.HasValue &&
                                todayData.SMA50 > todayData.SMA200 && yesterdayData.SMA50 <= yesterdayData.SMA200)
                            {
                                signalFired = true;
                            }
                        }

                        if (signalFired)
                        {
                            int sharesToBuy = (int)(amountToInvest / todayData.ClosePrice);
                            if (sharesToBuy > 0)
                            {
                                var newTrade = new SimulatedTrade { Ticker = ticker, EntryDate = currentDate, EntryPrice = todayData.ClosePrice, Shares = sharesToBuy };
                                virtualPortfolio.Add(newTrade);
                                availableCapital -= (newTrade.EntryPrice * newTrade.Shares);
                            }
                        }
                    }
                }
            }

            // 3. Finalize Positions
            onProgress("Simulation complete. Finalizing positions and calculating yearly stats...");
            foreach (var trade in virtualPortfolio)
            {
                if (trade.IsOpen)
                {
                    var finalData = marketDataLookup.ContainsKey(trade.Ticker) ? marketDataLookup[trade.Ticker].LastOrDefault(d => d.PriceDate <= parameters.EndDate) : null;
                    trade.ExitDate = parameters.EndDate;
                    trade.ExitPrice = finalData?.ClosePrice ?? trade.EntryPrice;
                    availableCapital += (decimal)(trade.ExitPrice * trade.Shares);
                }
            }
            result.AllTrades.AddRange(virtualPortfolio);
            result.EndingCapital = availableCapital;

            // 4. Post-processing for Year-by-Year Analysis
            var yearlyTrades = result.AllTrades.Where(t => t.ExitDate.HasValue).GroupBy(t => t.ExitDate!.Value.Year);
            var lastYearEndCapital = parameters.InitialCapital;

            for (int year = parameters.StartDate.Year; year <= parameters.EndDate.Year; year++)
            {
                var tradesInYear = yearlyTrades.FirstOrDefault(g => g.Key == year);
                var yearlyPL = tradesInYear?.Sum(t => t.ProfitLoss) ?? 0;
                var yearEndCapital = lastYearEndCapital + yearlyPL;
                result.YearlyBreakdown.Add(new YearlyPerformance
                {
                    Year = year,
                    TotalTrades = tradesInYear?.Count() ?? 0,
                    NetPL = yearlyPL,
                    YearEndCapital = yearEndCapital,
                    AnnualReturnPercent = lastYearEndCapital == 0 ? 0 : (double)(yearlyPL / lastYearEndCapital)
                });
                lastYearEndCapital = yearEndCapital;
            }

            stopwatch.Stop();
            onProgress($"Analysis complete! Total time: {stopwatch.Elapsed.TotalSeconds:N2} seconds.");
            return result;
        }

        private decimal CalculateAmountToInvest(Strategy strategy, decimal availableCapital, List<SimulatedTrade> portfolio, Dictionary<string, List<DailyDataPoint>> marketData, DateTime currentDate)
        {
            if (strategy.PositionSizingStrategy == "FixedAmount")
            {
                return strategy.PositionSizeValue;
            }

            if (strategy.PositionSizingStrategy == "PercentOfEquity")
            {
                decimal openPositionsValue = 0;
                foreach (var trade in portfolio.Where(t => t.IsOpen))
                {
                    decimal currentPrice = trade.EntryPrice; // Default to entry price
                    if (marketData.ContainsKey(trade.Ticker))
                    {
                        var latestDataPoint = marketData[trade.Ticker].FirstOrDefault(d => d.PriceDate == currentDate);
                        if (latestDataPoint != null)
                        {
                            currentPrice = latestDataPoint.ClosePrice;
                        }
                    }
                    openPositionsValue += currentPrice * trade.Shares;
                }

                decimal totalEquity = availableCapital + openPositionsValue;
                return totalEquity * (strategy.PositionSizeValue / 100);
            }

            return 0; // Default case
        }
    }
}