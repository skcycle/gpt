using MotionControl.Device.Abstractions.Results;

namespace MotionControl.Device.Abstractions.Controllers;

public interface ISafetyController
{
    Task<DeviceResult> RapidStopAsync(int mode, CancellationToken cancellationToken = default);
}
