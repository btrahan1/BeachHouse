using static BeachHouse.UI.Components.Pages.EquityChart;

namespace BeachHouse.UI.Models
{
    public class ChartData
    {
        public List<string> Labels { get; set; } = new();
        public List<ChartDataset> Datasets { get; set; } = new();
    }
}