using MotionControl.Device.Abstractions.Controllers;

namespace MotionControl.Control.Services;

public sealed class ControllerPollingService(
    IMotionController motionController,
    AxisPollingService axisPollingService,
    IoPollingService ioPollingService,
    AlarmPollingService alarmPollingService)
{
    private bool _isRunning;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return;
        }

        var result = await motionController.ConnectAsync(cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Controller connect failed: {result.ErrorMessage}");
        }

        _isRunning = true;
        await PollOnceAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        await motionController.DisconnectAsync(cancellationToken);
        _isRunning = false;
    }

    public async Task PollOnceAsync(CancellationToken cancellationToken = default)
    {
        await axisPollingService.PollAsync(cancellationToken);
        await ioPollingService.PollAsync(cancellationToken);
        await alarmPollingService.PollAsync(cancellationToken);
    }
}
