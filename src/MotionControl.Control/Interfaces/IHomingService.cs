using MotionControl.Domain.Entities;

namespace MotionControl.Control.Interfaces;

public interface IHomingService
{
    Task HomeAxisAsync(Axis axis, CancellationToken cancellationToken = default);
}
