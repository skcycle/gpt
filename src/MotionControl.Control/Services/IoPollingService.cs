using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class IoPollingService(Machine machine)
{
    public Task PollAsync(CancellationToken cancellationToken = default)
    {
        foreach (var ioPoint in machine.IoPoints)
        {
            ioPoint.Update(ioPoint.Value);
        }

        return Task.CompletedTask;
    }
}
