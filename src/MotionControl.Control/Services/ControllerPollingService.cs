using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class ControllerPollingService(
    IMotionController motionController,
    Machine machine,
    ControllerRuntimeState controllerRuntimeState,
    AxisPollingService axisPollingService,
    IoPollingService ioPollingService,
    AlarmPollingService alarmPollingService,
    SystemStateMachine systemStateMachine)
{
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private bool _isRunning;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return;
        }

        machine.SetSystemState(systemStateMachine.OnConnectingRequested());
        var result = await motionController.ConnectAsync(cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Controller connect failed: {result.ErrorMessage}");
        }

        _isRunning = true;
        machine.SetSystemState(systemStateMachine.OnSyncingRequested());
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
        if (!await _pollLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            await axisPollingService.PollAsync(cancellationToken);
            await ioPollingService.PollAsync(cancellationToken);

            var controllerStatus = await motionController.GetControllerStatusAsync(cancellationToken);
            controllerRuntimeState.Update(controllerStatus);
            await alarmPollingService.PollAsync(cancellationToken);
            var nextSystemState = systemStateMachine.OnPolling(machine, controllerStatus);
            machine.SetSystemState(nextSystemState);
        }
        finally
        {
            _pollLock.Release();
        }
    }
}
