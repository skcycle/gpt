using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.Homing;

public sealed class DefaultHomeStrategy : IHomeStrategy
{
    public HomeMode HomeMode => HomeMode.Default;

    public Task ExecuteAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        axis.ClearHomed();
        return Task.CompletedTask;
    }
}
