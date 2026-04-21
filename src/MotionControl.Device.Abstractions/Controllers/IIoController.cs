using MotionControl.Device.Abstractions.Results;

namespace MotionControl.Device.Abstractions.Controllers;

public interface IIoController
{
    Task<bool> GetIoPointValueAsync(int address, bool isOutput, CancellationToken cancellationToken = default);
    Task<DeviceResult> SetIoPointValueAsync(int address, bool value, CancellationToken cancellationToken = default);
}
