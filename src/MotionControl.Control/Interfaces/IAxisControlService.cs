using MotionControl.Domain.Entities;

namespace MotionControl.Control.Interfaces;

public interface IAxisControlService
{
    Task EnableAxisAsync(Axis axis, CancellationToken cancellationToken = default);
    Task DisableAxisAsync(Axis axis, CancellationToken cancellationToken = default);
    Task MoveAbsoluteAsync(Axis axis, double position, double velocity, double acceleration, double deceleration, CancellationToken cancellationToken = default);
    Task JogAsync(Axis axis, double velocity, bool positiveDirection, CancellationToken cancellationToken = default);
    Task StopAsync(Axis axis, CancellationToken cancellationToken = default);
    Task ResetAlarmAsync(Axis axis, CancellationToken cancellationToken = default);
}
