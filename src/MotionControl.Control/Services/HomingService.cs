using MotionControl.Control.Interfaces;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class HomingService(IMotionController motionController) : IHomingService
{
    public async Task HomeAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var result = await motionController.HomeAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Home axis failed: {result.ErrorMessage}");
        }

        axis.MarkHomed();
    }
}
