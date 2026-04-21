using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Results;

namespace MotionControl.Control.Services;

public sealed class IoControlService(IIoController motionController)
{
    public Task<DeviceResult> SetOutputAsync(int address, bool value, CancellationToken cancellationToken = default)
    {
        return motionController.SetIoPointValueAsync(address, value, cancellationToken);
    }
}
