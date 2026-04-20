using MotionControl.Application.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Control.StateMachines;
using MotionControl.Domain.Entities;

namespace MotionControl.Application.Services;

public sealed class SystemAppService(
    Machine machine,
    ControllerPollingService controllerPollingService,
    SystemStateMachine systemStateMachine) : ISystemAppService
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
}
