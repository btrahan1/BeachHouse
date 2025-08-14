namespace BeachHouse.UI.Models
{
    public class ScreenerResult
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal ClosePrice { get; set; }
        public long Volume { get; set; }
        public decimal? SMA50 { get; set; }
        public decimal? SMA200 { get; set; }
    }
}
