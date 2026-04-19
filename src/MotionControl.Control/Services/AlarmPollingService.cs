using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class AlarmPollingService(Machine machine)
{
    public Task PollAsync(CancellationToken cancellationToken = default)
    {
        // TODO: 这里后续接真实报警字、故障码、控制器状态字解析。
        _ = machine;
        return Task.CompletedTask;
    }
}
