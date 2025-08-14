namespace BeachHouse.UI.Models
{
    public class BacktestResult
    {
        public List<SimulatedTrade> AllTrades { get; set; } = new();
        public decimal EndingCapital { get; set; }
        public decimal InitialCapital { get; set; }

        /// <summary>
        /// NEW: Holds the year-by-year performance breakdown.
        /// </summary>
        public List<YearlyPerformance> YearlyBreakdown { get; set; } = new();

        public decimal NetPL => EndingCapital - InitialCapital;
        public decimal NetPLPercent => InitialCapital == 0 ? 0 : NetPL / InitialCapital;
        public int TotalTrades => AllTrades.Count(t => t.ExitDate.HasValue);
        public int WinningTrades => AllTrades.Count(t => t.ProfitLoss > 0);
        public int LosingTrades => AllTrades.Count(t => t.ProfitLoss < 0);
        public double WinRate => TotalTrades == 0 ? 0 : (double)WinningTrades / TotalTrades;
        public decimal AverageGain => AllTrades.Where(t => t.ProfitLoss > 0).DefaultIfEmpty().Average(t => t?.ProfitLoss ?? 0);
        public decimal AverageLoss => AllTrades.Where(t => t.ProfitLoss < 0).DefaultIfEmpty().Average(t => t?.ProfitLoss ?? 0);
        public decimal ProfitFactor => Math.Abs(AllTrades.Where(t => t.ProfitLoss < 0).Sum(t => t.ProfitLoss)) == 0 ? 999 : AllTrades.Where(t => t.ProfitLoss > 0).Sum(t => t.ProfitLoss) / Math.Abs(AllTrades.Where(t => t.ProfitLoss < 0).Sum(t => t.ProfitLoss));
    }
}