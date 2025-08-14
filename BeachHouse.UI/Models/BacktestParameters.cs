namespace BeachHouse.UI.Models
{
    /// <summary>
    /// Holds the user-defined parameters for a single backtest run.
    /// The strategy logic itself is loaded from the database.
    /// </summary>
    public class BacktestParameters
    {
        public DateTime StartDate { get; set; } = new DateTime(2000, 1, 1);
        public DateTime EndDate { get; set; } = new DateTime(2017, 12, 31);
        public decimal InitialCapital { get; set; } = 100_000;

        // --- NEW Properties for Seasonal Backtesting ---
        public string Mode { get; set; } = "Single"; // "Single" or "Seasonal"
        public int Q1StrategyId { get; set; }
        public int Q2StrategyId { get; set; }
        public int Q3StrategyId { get; set; }
        public int Q4StrategyId { get; set; }

        /// <summary>
        /// The ID of the strategy from the database to be tested (used in Single mode).
        /// </summary>
        public int StrategyId { get; set; }
    }
}
