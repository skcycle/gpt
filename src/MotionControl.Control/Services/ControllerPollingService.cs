using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Results;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.Services;

public sealed class ControllerPollingService(
    IEtherCatController motionController,
    Machine machine,
    ControllerRuntimeState controllerRuntimeState,
    AxisPollingService axisPollingService,
    IoPollingService ioPollingService,
    AlarmPollingService alarmPollingService,
    SystemStateMachine systemStateMachine,
    CommandFeedbackRuntimeState commandFeedbackRuntimeState)
{
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private bool _isRunning;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return;
        }

        var connectingState = systemStateMachine.OnConnectingRequested();
        if (machine.CurrentState != connectingState)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "SystemState", Status = "Changed", Message = $"{machine.CurrentState} -> {connectingState}" });
            machine.SetSystemState(connectingState);
        }
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectCts.CancelAfter(TimeSpan.FromSeconds(10));
        var result = DeviceResult.Fail("Connect never attempted");
        for (var attempt = 0; attempt < 3 && !result.Success; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
            result = await motionController.ConnectAsync(connectCts.Token);
        }
        if (!result.Success)
        {
            machine.SetConnected(false);
            machine.UpsertAlarm("SYS-CONTROLLER-DISCONNECTED", $"Controller connect failed: {result.ErrorMessage}", "System", "Communication", "Error");
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Connect", Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
            machine.SetSystemState(SystemState.Fault);
            _isRunning = false;
            return;
        }

        machine.SetConnected(true);
        _isRunning = true;
        var syncingState = systemStateMachine.OnSyncingRequested();
        if (machine.CurrentState != syncingState)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "SystemState", Status = "Changed", Message = $"{machine.CurrentState} -> {syncingState}" });
            machine.SetSystemState(syncingState);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        await motionController.DisconnectAsync(cancellationToken);
        machine.SetConnected(false);
        _isRunning = false;
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    public async Task PollOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        if (!await _pollLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            await axisPollingService.PollAsync(cancellationToken);
            await ioPollingService.PollAsync(cancellationToken);
            var controllerStatus = await motionController.GetControllerStatusAsync(cancellationToken);
            machine.SetConnected(controllerStatus.IsConnected);
            controllerRuntimeState.Update(controllerStatus);
            await alarmPollingService.PollAsync(cancellationToken);

            var previousSystemState = machine.CurrentState;
            var nextSystemState = systemStateMachine.OnPolling(machine, controllerStatus);
            if (previousSystemState != nextSystemState)
            {
                commandFeedbackRuntimeState.Add(new CommandFeedback
                {
                    CommandName = "SystemState",
                    Status = "Changed",
                    Message = $"{previousSystemState} -> {nextSystemState}"
                });
                machine.SetSystemState(nextSystemState);
            }
        }
        catch
        {
            machine.SetConnected(false);
            controllerRuntimeState.Update(new MotionControl.Device.Abstractions.Models.EtherCatControllerStatus
            {
                IsConnected = false,
                NetworkState = "Disconnected",
                OnlineSlaveCount = 0,
                Slaves = Array.Empty<MotionControl.Device.Abstractions.Models.EtherCatSlaveStatus>()
            });
            // 控制器断连等异常不打断轮询，让上层决定是否停止
        }
        finally
        {
            _pollLock.Release();
        }
    }
}
