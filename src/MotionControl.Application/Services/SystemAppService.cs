using MotionControl.Application.Interfaces;
using MotionControl.Control.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Control.StateMachines;
using MotionControl.Domain.Entities;

namespace MotionControl.Application.Services;

public sealed class SystemAppService(
    Machine machine,
    ControllerPollingService controllerPollingService,
    SystemStateMachine systemStateMachine,
    IAxisControlService axisControlService) : ISystemAppService
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        machine.SetSystemState(systemStateMachine.OnInitializeRequested());
        await controllerPollingService.StartAsync(cancellationToken);
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return controllerPollingService.PollOnceAsync(cancellationToken);
    }

    public async Task EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        machine.SetSystemState(systemStateMachine.OnEmergencyStopRequested());

        foreach (var axis in machine.Axes)
        {
            try
            {
                await axisControlService.StopAsync(axis, cancellationToken);
            }
            catch
            {
                // Keep issuing stop to remaining axes during emergency stop.
            }
        }

        await controllerPollingService.PollOnceAsync(cancellationToken);
    }

    public async Task ClearEmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        machine.SetSystemState(systemStateMachine.OnEmergencyStopCleared(machine, null));
        await controllerPollingService.PollOnceAsync(cancellationToken);
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        await controllerPollingService.ReconnectAsync(cancellationToken);
    }
}
