using MotionControl.Application.DTOs;

namespace MotionControl.Application.Interfaces;

public interface IMotionAppService
{
    Task EnableAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default);
    Task HomeAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default);
    Task MoveAbsoluteAsync(MoveAxisCommandDto command, CancellationToken cancellationToken = default);
    Task StopAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default);
    Task JogAxisAsync(JogAxisCommandDto command, CancellationToken cancellationToken = default);
}
