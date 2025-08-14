using CsvHelper.Configuration.Attributes;

namespace BeachHouse.UI.Models
{
    public class DailyPriceHistory
    {
        // This property is not in the CSV but will be populated from the filename.
        [Ignore]
        public string Ticker { get; set; } = string.Empty;

        [Name("Date")]
        public DateTime PriceDate { get; set; }

        [Name("Open")]
        public decimal OpenPrice { get; set; }

        [Name("High")]
        public decimal HighPrice { get; set; }

        [Name("Low")]
        public decimal LowPrice { get; set; }

        [Name("Close")]
        public decimal ClosePrice { get; set; }

        [Name("Volume")]
        public long Volume { get; set; }

        // The 'OpenInt' column from the CSV will be ignored by CsvHelper.
    }
}
