using MotionControl.Application.Interfaces;
using MotionControl.Device.Zmc.Native;

namespace MotionControl.Application.Services;

public sealed class AxisControllerParameterAppService(ZmcAxisNativeFacade axisNativeFacade) : IAxisControllerParameterAppService
{
    public Task<string> ReadControllerParametersAsync(int axisNo, CancellationToken cancellationToken = default)
    {
        var response = axisNativeFacade.ReadAxisParameters(axisNo);
        return Task.FromResult(response);
    }

    public Task WriteControllerParametersAsync(int axisNo, double workVelocity, double setupVelocity, double pulseEquivalent, CancellationToken cancellationToken = default)
    {
        var result = axisNativeFacade.WriteAxisParameters(axisNo, workVelocity, setupVelocity, pulseEquivalent);
        if (result != 0)
        {
            throw new InvalidOperationException($"Write controller parameters failed: {result}");
        }

        return Task.CompletedTask;
    }
}
