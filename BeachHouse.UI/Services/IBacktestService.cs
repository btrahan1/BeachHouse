using BeachHouse.UI.Models;
namespace BeachHouse.UI.Services
{
    public interface IBacktestService
    {
        Task<BacktestResult> RunBacktestAsync(BacktestParameters parameters, bool onlySP500, Action<string> onProgress, Action<int> onProgressPercentageUpdate);
        Task<IEnumerable<Strategy>> GetAllStrategiesAsync();
        Task SaveBacktestResultAsync(BacktestParameters parameters, BacktestResult result, string notes);
    }
}
