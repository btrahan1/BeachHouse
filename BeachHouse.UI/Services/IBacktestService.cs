using BeachHouse.UI.Models;
namespace BeachHouse.UI.Services
{
    public interface IBacktestService
    {
        Task<BacktestResult> RunBacktestAsync(BacktestParameters parameters, bool onlySP500, Action<string> onProgress);
        Task<IEnumerable<Strategy>> GetAllStrategiesAsync();
    }
}
