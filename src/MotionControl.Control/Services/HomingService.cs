using MotionControl.Control.Homing;
using MotionControl.Control.Interfaces;
using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Results;
using MotionControl.Diagnostics.Services;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class HomingService(
    IAxisMotionController motionController,
    IEnumerable<IHomeStrategy> homeStrategies,
    HomePlanRuntimeState homePlanRuntimeState,
    CommandFeedbackRuntimeState commandFeedbackRuntimeState,
    AxisStateMachine axisStateMachine,
    SafetyInterlockService safetyInterlock) : IHomingService
{
    public async Task<DeviceResult> HomeAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        if (!safetyInterlock.CanStartHome(axis))
            return DeviceResult.Fail($"轴 {axis.Name} 有报警，无法回零");

        var strategy = homeStrategies.FirstOrDefault(item => item.HomeMode == axis.HomeMode)
            ?? homeStrategies.First(item => item.HomeMode == MotionControl.Domain.Enums.HomeMode.Default);

        var plan = strategy.BuildPlan(axis);
        homePlanRuntimeState.Update(plan);

        axis.ApplyState(axisStateMachine.OnHomeIssued());
        commandFeedbackRuntimeState.AddStarted("Home", axis.ControllerAxisNo, plan.Title);
        var result = await motionController.HomeAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.AddFailed("Home", axis.ControllerAxisNo, result.ErrorMessage ?? "Unknown error");
            return DeviceResult.Fail($"轴 {axis.Id.Value} ({axis.Name}) 回零失败: {result.ErrorMessage}");
        }

        await strategy.ExecuteAsync(axis, cancellationToken);
        axis.ApplyState(axisStateMachine.OnHomeSucceeded(axis));
        commandFeedbackRuntimeState.AddSucceeded("Home", axis.ControllerAxisNo, strategy.BuildPlan(axis).Title);
        return DeviceResult.Ok();
    }
}
