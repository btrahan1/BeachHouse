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

        /// <summary>
        /// The ID of the strategy from the database to be tested.
        /// </summary>
        public int StrategyId { get; set; }
    }
}
