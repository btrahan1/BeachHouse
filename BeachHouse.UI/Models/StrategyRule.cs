namespace BeachHouse.UI.Models
{
    public class StrategyRule
    {
        public int StrategyRuleId { get; set; }
        public int StrategyId { get; set; }
        public string RuleType { get; set; } = string.Empty; // "Entry" or "Exit"
        public string SignalName { get; set; } = string.Empty;
    }
}
