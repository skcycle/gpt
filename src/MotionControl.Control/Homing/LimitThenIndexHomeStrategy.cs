using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.Homing;

public sealed class LimitThenIndexHomeStrategy : IHomeStrategy
{
    public HomeMode HomeMode => HomeMode.LimitThenIndex;

    public Task ExecuteAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        axis.MarkHomed();
        return Task.CompletedTask;
    }
}
