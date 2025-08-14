using BeachHouse.UI.Models;
using System.Collections.ObjectModel;

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

            (MinSimulationDate, MaxSimulationDate) = await _db.GetDataDateRangeAsync();
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

            var transactionData = await _db.LoadData<TransactionHistory, dynamic>("spTransactionHistory_GetAll", new { });
            Transactions.Clear();
            foreach (var tx in transactionData) { Transactions.Add(tx); }
        }

        public async Task ExecuteBuyOrderAsync(string ticker, string companyName, int shares, decimal pricePerShare)
        {
            await _db.SaveData("spTransaction_Buy", new { ticker, companyName, shares, pricePerShare, transactionDate = CurrentSimulationDate });
            await LoadAllDataAsync();
            OnBrokerageChanged?.Invoke();
        }

        public async Task ExecuteSellOrderAsync(string ticker, int shares, decimal pricePerShare)
        {
            await _db.SaveData("spTransaction_Sell", new { ticker, shares, pricePerShare, transactionDate = CurrentSimulationDate });
            await LoadAllDataAsync();
            OnBrokerageChanged?.Invoke();
        }

        public async Task ExecuteDepositAsync(decimal amount)
        {
            await _db.SaveData("spTransaction_Deposit", new { amount });
            await LoadAllDataAsync();
            OnBrokerageChanged?.Invoke();
        }

        public async Task ExecuteWithdrawalAsync(decimal amount)
        {
            await _db.SaveData("spTransaction_Withdraw", new { amount });
            await LoadAllDataAsync();
            OnBrokerageChanged?.Invoke();
        }
    }
}
