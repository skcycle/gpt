using MotionControl.Application.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Application.Services;

public sealed class SystemAppService(
    Machine machine,
    ControllerPollingService controllerPollingService) : ISystemAppService
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        machine.SetSystemState(SystemState.Initializing);
        await controllerPollingService.StartAsync(cancellationToken);
        machine.SetSystemState(SystemState.Ready);
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return controllerPollingService.PollOnceAsync(cancellationToken);
    }
}
