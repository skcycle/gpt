using MotionControl.Control.Homing;
using MotionControl.Control.Interfaces;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class HomingService(
    IMotionController motionController,
    IEnumerable<IHomeStrategy> homeStrategies,
    HomePlanRuntimeState homePlanRuntimeState) : IHomingService
{
    public async Task HomeAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var strategy = homeStrategies.FirstOrDefault(item => item.HomeMode == axis.HomeMode)
            ?? homeStrategies.First(item => item.HomeMode == MotionControl.Domain.Enums.HomeMode.Default);

        var plan = strategy.BuildPlan(axis);
        homePlanRuntimeState.Update(plan);

        var result = await motionController.HomeAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Home axis failed: {result.ErrorMessage}");
        }

        await strategy.ExecuteAsync(axis, cancellationToken);
    }
}
