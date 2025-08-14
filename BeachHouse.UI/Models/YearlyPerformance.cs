namespace BeachHouse.UI.Models
{
    public class YearlyPerformance
    {
        public int Year { get; set; }
        public decimal NetPL { get; set; }
        public int TotalTrades { get; set; }
        public decimal YearEndCapital { get; set; }
        public double AnnualReturnPercent { get; set; }
    }
}