using MotionControl.Device.Abstractions.Controllers;

namespace MotionControl.Control.Services;

public sealed class IoControlService(IMotionController motionController)
{
    public async Task SetOutputAsync(int address, bool value, CancellationToken cancellationToken = default)
    {
        await motionController.SetIoPointValueAsync(address, value, cancellationToken);
    }
}
