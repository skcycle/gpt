using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    CommandFeedbackRuntimeState commandFeedbackRuntimeState,
    ILogger<ControllerPollingService>? logger = null)
{
    private readonly ILogger<ControllerPollingService> _logger = logger ?? NullLogger<ControllerPollingService>.Instance;
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private readonly object _startStopLock = new();
    private static readonly TimeSpan[] ReconnectBackoff = { TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10) };
    private int _consecutiveFailures;
    private int _reconnectInProgress;
    private bool _isRunning;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_startStopLock)
        {
            if (_isRunning) return;
        }

        _logger.LogInformation("Controller polling starting…");

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
                _logger.LogError("Controller connect failed after all retries: {Error}", result.ErrorMessage);
                machine.SetConnected(false);
                machine.UpsertAlarm("SYS-CONTROLLER-DISCONNECTED", $"Controller connect failed: {result.ErrorMessage}", "System", "Communication", "Error");
                commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Connect", Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
                machine.SetSystemState(SystemState.Fault);
                _isRunning = false;
                return;
            }

            _logger.LogInformation("Controller connected successfully");
            machine.SetConnected(true);
            _isRunning = true;
            _consecutiveFailures = 0;
            _reconnectInProgress = 0;
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

        _logger.LogInformation("Controller polling stopping");
        await motionController.DisconnectAsync(cancellationToken);
        machine.SetConnected(false);
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Controller reconnect initiated");
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
                _logger.LogDebug("System state transition: {Previous} -> {Next}", previousSystemState, nextSystemState);
                commandFeedbackRuntimeState.Add(new CommandFeedback
                {
                    CommandName = "SystemState",
                    Status = "Changed",
                    Message = $"{previousSystemState} -> {nextSystemState}"
                });
                machine.SetSystemState(nextSystemState);
            }

            _consecutiveFailures = 0;
            _reconnectInProgress = 0;
        }
        catch (Exception ex)
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            _logger.LogWarning(ex, "Poll cycle failed (consecutive failures={Failures})", failures);
            machine.SetConnected(false);
            controllerRuntimeState.Update(new MotionControl.Device.Abstractions.Models.EtherCatControllerStatus
            {
                IsConnected = false,
                NetworkState = "Disconnected",
                OnlineSlaveCount = 0,
                Slaves = Array.Empty<MotionControl.Device.Abstractions.Models.EtherCatSlaveStatus>(),
            });

            if (ShouldAttemptReconnect(failures)
                && Interlocked.CompareExchange(ref _reconnectInProgress, 1, 0) == 0)
            {
                var delayIndex = Math.Min(failures - 1, ReconnectBackoff.Length - 1);
                var delay = ReconnectBackoff[delayIndex];
                _logger.LogWarning("Scheduling reconnect attempt in {DelayMs}ms (failures={Failures})",
                    (int)delay.TotalMilliseconds, failures);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delay);
                        _logger.LogInformation("Reconnect attempt starting");
                        await ReconnectAsync(CancellationToken.None);
                        _logger.LogInformation("Reconnect succeeded");
                    }
                    catch (Exception reconnectEx)
                    {
                        _logger.LogError(reconnectEx, "Reconnect attempt failed");
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _reconnectInProgress, 0);
                    }
                });
            }
        }
        finally
        {
            _pollLock.Release();
        }
    }

    private static bool ShouldAttemptReconnect(int consecutiveFailures)
        => consecutiveFailures > 1 && consecutiveFailures % 4 == 0;
}
