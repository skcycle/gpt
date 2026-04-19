using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class ControllerPollingService(IMotionController motionController, Machine machine)
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
        foreach (var axis in machine.Axes)
        {
            var feedback = await motionController.GetAxisFeedbackAsync(axis.ControllerAxisNo, cancellationToken);
            axis.UpdateFeedback(
                feedback.CurrentPosition,
                feedback.CurrentVelocity,
                feedback.AxisState,
                feedback.ServoState,
                feedback.HasAlarm,
                feedback.PositiveLimitTriggered,
                feedback.NegativeLimitTriggered);
        }
    }
}
