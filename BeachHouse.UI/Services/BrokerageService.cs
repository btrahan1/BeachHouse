using BeachHouse.UI.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace BeachHouse.UI.Services
{
    public class BrokerageService
    {
        private readonly SqlDataAccess _db;
        private bool _isInitialized = false;

        public decimal CashBalance { get; private set; }
        public ObservableCollection<StockHolding> Holdings { get; private set; } = new();
        public ObservableCollection<TransactionHistory> Transactions { get; private set; } = new();
        
        public DateTime CurrentSimulationDate { get; private set; }
        public DateTime MinSimulationDate { get; private set; }
        public DateTime MaxSimulationDate { get; private set; }

        public event Action? OnBrokerageChanged;
        public event Action? OnSimulationDateChanged;

        public BrokerageService(SqlDataAccess db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            await LoadAllDataAsync();

            var (minDate, maxDate) = await _db.GetDataDateRangeAsync();
            MinSimulationDate = minDate;
            MaxSimulationDate = maxDate;

            // Handle case where database is empty
            if (MaxSimulationDate < MinSimulationDate || MaxSimulationDate == default)
            {
                MaxSimulationDate = DateTime.Today;
                MinSimulationDate = DateTime.Today.AddYears(-1);
            }
            CurrentSimulationDate = MaxSimulationDate;

            _isInitialized = true;
            OnBrokerageChanged?.Invoke();
        }

        public void SetSimulationDate(DateTime newDate)
        {
            if (newDate >= MinSimulationDate && newDate <= MaxSimulationDate)
            {
                CurrentSimulationDate = newDate;
                OnSimulationDateChanged?.Invoke();
            }
        }

        private async Task LoadAllDataAsync()
        {
            var cashData = await _db.LoadData<decimal, dynamic>("spAccount_GetBalance", new { });
            CashBalance = cashData.FirstOrDefault();

            var holdingData = await _db.LoadData<StockHolding, dynamic>("spStockHolding_GetAll", new { });
            Holdings.Clear();
            foreach (var holding in holdingData) { Holdings.Add(holding); }

            await ReloadTransactionsAsync();
        }

        private async Task ReloadTransactionsAsync()
        {
            var transactionData = await _db.LoadData<TransactionHistory, dynamic>("spTransactionHistory_GetAll", new { });
            Transactions.Clear();
            foreach (var tx in transactionData) { Transactions.Add(tx); }
        }

        public async Task ExecuteBuyOrderAsync(string ticker, string companyName, int shares, decimal pricePerShare)
        {
            await _db.SaveData("spTransaction_Buy", new { ticker, companyName, shares, pricePerShare, transactionDate = CurrentSimulationDate });

            // Optimistically update in-memory state
            decimal totalCost = (decimal)shares * pricePerShare;
            CashBalance -= totalCost;

            var existingHolding = Holdings.FirstOrDefault(h => h.Ticker == ticker);
            if (existingHolding != null)
            {
                var newAverageCost = ((decimal)existingHolding.Shares * existingHolding.AverageCost + totalCost) / (existingHolding.Shares + shares);
                existingHolding.Shares += shares;
                existingHolding.AverageCost = newAverageCost;
            }
            else
            {
                var newHolding = new StockHolding 
                { 
                    Ticker = ticker, 
                    CompanyName = companyName, 
                    Shares = shares, 
                    AverageCost = pricePerShare, 
                    CurrentPrice = pricePerShare
                };
                Holdings.Add(newHolding);
            }

            await ReloadTransactionsAsync();
            OnBrokerageChanged?.Invoke();
        }

        public async Task ExecuteSellOrderAsync(string ticker, int shares, decimal pricePerShare)
        {
            await _db.SaveData("spTransaction_Sell", new { ticker, sharesToSell = shares, pricePerShare, transactionDate = CurrentSimulationDate });
            
            decimal totalProceeds = (decimal)shares * pricePerShare;
            CashBalance += totalProceeds;

            var existingHolding = Holdings.FirstOrDefault(h => h.Ticker == ticker);
            if (existingHolding != null)
            {
                existingHolding.Shares -= shares;
                if (existingHolding.Shares <= 0)
                {
                    Holdings.Remove(existingHolding);
                }
            }

            await ReloadTransactionsAsync();
            OnBrokerageChanged?.Invoke();
        }

        public async Task ExecuteDepositAsync(decimal amount)
        {
            await _db.SaveData("spTransaction_Deposit", new { amount, transactionDate = CurrentSimulationDate });

            CashBalance += amount;
            
            await ReloadTransactionsAsync();
            OnBrokerageChanged?.Invoke();
        }

        public async Task ExecuteWithdrawalAsync(decimal amount)
        {
            await _db.SaveData("spTransaction_Withdraw", new { amount, transactionDate = CurrentSimulationDate });

            CashBalance -= amount;
            
            await ReloadTransactionsAsync();
            OnBrokerageChanged?.Invoke();
        }
    }
}