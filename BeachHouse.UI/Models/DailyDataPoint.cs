namespace BeachHouse.UI.Models
{
    public class DailyDataPoint
    {
        public string Ticker { get; set; } = string.Empty;
        public DateTime PriceDate { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal? SMA50 { get; set; }
        public decimal? SMA200 { get; set; }
    }
}