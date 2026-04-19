using Microsoft.Extensions.Hosting;
using MotionControl.Device.Zmc.Config;

namespace MotionControl.Control.Services;

public sealed class PollingHostedService(
    ControllerPollingService controllerPollingService,
    ZmcControllerOptions zmcOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(zmcOptions.PollingIntervalMs <= 0 ? 200 : zmcOptions.PollingIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            await controllerPollingService.PollOnceAsync(stoppingToken);
            await Task.Delay(interval, stoppingToken);
        }
    }
}
