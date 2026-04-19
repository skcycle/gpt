using MotionControl.Presentation.ViewModels;

namespace MotionControl.Control.Services;

public sealed class PollingHostedService(
    ControllerPollingService controllerPollingService,
    MainWindowViewModel mainWindowViewModel)
{
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public void Start(TimeSpan interval)
    {
        if (_loopTask is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunLoopAsync(interval, _cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is null || _loopTask is null)
        {
            return;
        }

        _cts.Cancel();
        await _loopTask;
        _cts.Dispose();
        _cts = null;
        _loopTask = null;
    }

    private async Task RunLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await controllerPollingService.PollOnceAsync(cancellationToken);
            mainWindowViewModel.RefreshViewModels();
            await Task.Delay(interval, cancellationToken);
        }
    }
}
