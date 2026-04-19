using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MotionControl.Device.Zmc.Config;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.Control.Services;

public sealed class PollingHostedService(
    ControllerPollingService controllerPollingService,
    MainWindowViewModel mainWindowViewModel,
    IUiRefreshNotifier uiRefreshNotifier,
    IOptions<ZmcControllerOptions> zmcOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(zmcOptions.Value.PollingIntervalMs <= 0 ? 200 : zmcOptions.Value.PollingIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            await controllerPollingService.PollOnceAsync(stoppingToken);
            uiRefreshNotifier.RequestRefresh(mainWindowViewModel.RefreshViewModels);
            await Task.Delay(interval, stoppingToken);
        }
    }
}
