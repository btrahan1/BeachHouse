namespace BeachHouse.UI.Models
{
    public class StockHolding
    {
        public string Ticker { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int Shares { get; set; }
        public decimal AverageCost { get; set; }

        // --- Live/Simulated Data ---
        public decimal CurrentPrice { get; set; }
        public decimal MarketValue => Shares * CurrentPrice;

        // --- Calculated Performance Metrics ---
        public decimal UnrealizedPL => (CurrentPrice * Shares) - (AverageCost * Shares);
        public decimal UnrealizedPLPercent
        {
            get
            {
                if (AverageCost == 0) return 0;
                return (CurrentPrice - AverageCost) / AverageCost;
            }
        }
    }
}
