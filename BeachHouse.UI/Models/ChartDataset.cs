namespace BeachHouse.UI.Models
{
    public class ChartDataset
    {
        public string Label { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new();
        public string BorderColor { get; set; } = "#007bff";
        public double Tension { get; set; } = 0.1;
    }
}