namespace BeachHouse.UI.Models
{
    public class BacktestRun
    {
        public int BacktestRunId { get; set; }
        public DateTime RunDateTime { get; set; }
        public string ParametersJson { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal EndingCapital { get; set; }
        public decimal NetPL { get; set; }
        public int TotalTrades { get; set; }
        public double WinRate { get; set; }
    }
}
