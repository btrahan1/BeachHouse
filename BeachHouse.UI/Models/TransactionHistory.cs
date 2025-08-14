namespace BeachHouse.UI.Models;

// Represents a single, immutable transaction record.
public class TransactionHistory
{
    public int TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public double? Shares { get; set; }
    public decimal? PricePerShare { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}
