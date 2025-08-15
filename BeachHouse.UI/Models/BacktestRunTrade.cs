namespace BeachHouse.UI.Models
{
    public class BacktestRunTrade
    {
        public long BacktestRunTradeId { get; set; }
        public int BacktestRunId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int Shares { get; set; }
        public DateTime EntryDate { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime ExitDate { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal ProfitLoss { get; set; }
    }
}
