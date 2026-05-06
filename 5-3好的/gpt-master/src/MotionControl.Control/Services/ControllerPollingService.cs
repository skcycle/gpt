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
    private readonly object _startStopLock = new();
    private static readonly TimeSpan[] ReconnectBackoff = { TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10) };
    private int _consecutiveFailures;
    private bool _isRunning;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_startStopLock)
        {
            if (_isRunning) return;
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
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            result = await motionController.ConnectAsync(connectCts.Token);
        }

        lock (_startStopLock)
        {
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
            _consecutiveFailures = 0;
        }

        var syncingState = systemStateMachine.OnSyncingRequested();
        if (machine.CurrentState != syncingState)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "SystemState", Status = "Changed", Message = $"{machine.CurrentState} -> {syncingState}" });
            machine.SetSystemState(syncingState);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_startStopLock)
        {
            if (!_isRunning) return;
            _isRunning = false;
        }

        await motionController.DisconnectAsync(cancellationToken);
        machine.SetConnected(false);
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    public async Task PollOnceAsync(CancellationToken cancellationToken = default)
    {
        lock (_startStopLock)
        {
            if (!_isRunning) return;
        }

        if (!await _pollLock.WaitAsync(0, cancellationToken))
            return;

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

            // 正常轮询成功，重置连续失败计数
            _consecutiveFailures = 0;
        }
        catch
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            machine.SetConnected(false);
            controllerRuntimeState.Update(new MotionControl.Device.Abstractions.Models.EtherCatControllerStatus
            {
                IsConnected = false,
                NetworkState = "Disconnected",
                OnlineSlaveCount = 0,
                Slaves = Array.Empty<MotionControl.Device.Abstractions.Models.EtherCatSlaveStatus>(),
            });

            // 连续失败达到阈值时尝试重连（带退避）
            if (ShouldAttemptReconnect(failures))
            {
                var delayIndex = Math.Min(failures - 1, ReconnectBackoff.Length - 1);
                var delay = ReconnectBackoff[delayIndex];
                _ = Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    try { await ReconnectAsync(CancellationToken.None); }
                    catch { /* 重连失败交给下一轮轮询处理 */ }
                });
            }
        }
        finally
        {
            _pollLock.Release();
        }
    }

    /// <summary>
    /// 每隔 4 次连续失败尝试一次重连（避免每次都重连造成网络风暴）。
    /// </summary>
    private static bool ShouldAttemptReconnect(int consecutiveFailures)
        => consecutiveFailures > 1 && consecutiveFailures % 4 == 0;
}
