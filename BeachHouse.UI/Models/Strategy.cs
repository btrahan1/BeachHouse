namespace BeachHouse.UI.Models
{
    public class Strategy
    {
        public int StrategyId { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string PositionSizingStrategy { get; set; } = string.Empty;
        public decimal PositionSizeValue { get; set; }

        public List<StrategyRule> Rules { get; set; } = new();
    }
}
