using System.IO.Compression;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using BeachHouse.UI.Models;

namespace BeachHouse.UI.Services
{
    public class DataIngestionService : IDataIngestionService
    {
        private readonly SqlDataAccess _sql;
        private const int BatchSize = 5000; // Process records in batches of 5000

        public DataIngestionService(SqlDataAccess sql)
        {
            _sql = sql;
        }

        /// <summary>
        /// NEW: Special-purpose ingestor for a simple two-column SPY csv file (date,close).
        /// </summary>
        public async Task IngestSpyDataAsync(Stream csvStream, Action<string> onProgress)
        {
            onProgress("Starting SPY data ingestion process...");

            var recordsToInsert = new List<DailyPriceHistory>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, config);

            // CsvHelper can read into an anonymous type if the headers match property names
            var rawRecords = csv.GetRecords<dynamic>();

            onProgress("Parsing and transforming SPY data...");
            foreach (var record in rawRecords)
            {
                var recordDict = (IDictionary<string, object>)record;
                if (DateTime.TryParse((string)recordDict["date"], out var priceDate) &&
                    decimal.TryParse((string)recordDict["close"], out var closePrice))
                {
                    recordsToInsert.Add(new DailyPriceHistory
                    {
                        Ticker = "SPY",
                        PriceDate = priceDate,
                        ClosePrice = closePrice,
                        OpenPrice = closePrice, // Populate OHL with Close price
                        HighPrice = closePrice,
                        LowPrice = closePrice,
                        Volume = 100000 // Placeholder volume
                    });
                }
            }

            if (recordsToInsert.Any())
            {
                onProgress($"Transformed {recordsToInsert.Count} records. Bulk inserting into database...");
                await _sql.BulkInsertDailyPriceHistory(recordsToInsert);
                onProgress("SPY data successfully ingested!");
            }
            else
            {
                onProgress("No valid SPY records found to ingest.");
            }
        }

        public async Task IngestStockDataFromZipAsync(Stream zipStream, Action<string> onProgress)
        {
            onProgress("Starting Kaggle zip ingestion process...");

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var csvEntries = archive.Entries.Where(e => e.FullName.EndsWith(".txt") && !e.FullName.Contains("spy.us")).ToList();

            int filesProcessed = 0;

            foreach (var entry in csvEntries)
            {
                var ticker = Path.GetFileNameWithoutExtension(entry.FullName).Replace(".us", "");
                onProgress($"Processing file {++filesProcessed} of {csvEntries.Count}: {ticker}.txt");

                var records = new List<DailyPriceHistory>(BatchSize);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null
                };

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, config);

                await foreach (var record in csv.GetRecordsAsync<DailyPriceHistory>())
                {
                    record.Ticker = ticker;
                    records.Add(record);

                    if (records.Count >= BatchSize)
                    {
                        await FlushBatchAsync(records, onProgress);
                    }
                }

                if (records.Count > 0)
                {
                    await FlushBatchAsync(records, onProgress);
                }
            }

            onProgress("Kaggle zip ingestion complete!");
        }

        private async Task FlushBatchAsync(List<DailyPriceHistory> batch, Action<string> onProgress)
        {
            if (!batch.Any()) return;

            var ticker = batch.First().Ticker;
            onProgress($"--> Inserting {batch.Count} records for {ticker}...");

            await _sql.BulkInsertDailyPriceHistory(batch);

            batch.Clear();
        }
    }
}