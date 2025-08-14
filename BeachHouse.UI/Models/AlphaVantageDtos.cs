using System.Text.Json.Serialization;

namespace BeachHouse.UI.Models
{
    // DTO for TIME_SERIES_DAILY_ADJUSTED endpoint
    public class AlphaVantageTimeSeriesResponse
    {
        [JsonPropertyName("Meta Data")]
        public MetaData? MetaData { get; set; }

        [JsonPropertyName("Time Series (Daily)")]
        public Dictionary<string, DailyAdjustedDataPoint>? TimeSeriesDaily { get; set; }
    }

    public class MetaData
    {
        [JsonPropertyName("2. Symbol")]
        public string Symbol { get; set; }
    }

    public class DailyAdjustedDataPoint
    {
        [JsonPropertyName("4. close")]
        public string Close { get; set; }
    }

    // DTOs for GLOBAL_QUOTE endpoint
    public class GlobalQuoteResponse
    {
        [JsonPropertyName("Global Quote")]
        public GlobalQuote? Quote { get; set; }
    }

    public class GlobalQuote
    {
        [JsonPropertyName("01. symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("05. price")]
        public string Price { get; set; }

        [JsonPropertyName("09. change")]
        public string Change { get; set; }

        [JsonPropertyName("10. change percent")]
        public string ChangePercent { get; set; }
    }

    // --- NEW DTOs for SYMBOL_SEARCH endpoint ---
    public class SymbolSearchResponse
    {
        [JsonPropertyName("bestMatches")]
        public List<SymbolSearchMatch>? BestMatches { get; set; }
    }

    public class SymbolSearchMatch
    {
        [JsonPropertyName("1. symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("2. name")]
        public string Name { get; set; }

        [JsonPropertyName("4. region")]
        public string Region { get; set; }
    }
}
