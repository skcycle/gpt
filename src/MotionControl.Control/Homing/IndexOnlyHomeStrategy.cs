using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.Homing;

public sealed class IndexOnlyHomeStrategy : IHomeStrategy
{
    public HomeMode HomeMode => HomeMode.IndexOnly;

    public Task ExecuteAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        axis.MarkHomed();
        return Task.CompletedTask;
    }
}
