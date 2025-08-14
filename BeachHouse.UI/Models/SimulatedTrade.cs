namespace BeachHouse.UI.Models
{
    public class SimulatedTrade
    {
        public string Ticker { get; set; } = string.Empty;
        public int Shares { get; set; }
        public DateTime EntryDate { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime? ExitDate { get; set; }
        public decimal? ExitPrice { get; set; }
        public bool IsOpen => ExitDate == null;

        public decimal ProfitLoss => IsOpen ? 0 : (decimal)((ExitPrice - EntryPrice) * Shares);
        public decimal ProfitLossPercent
        {
            get
            {
                if (IsOpen || EntryPrice == 0) return 0;
                return (decimal)((ExitPrice - EntryPrice) / EntryPrice);
            }
        }
    }
}
