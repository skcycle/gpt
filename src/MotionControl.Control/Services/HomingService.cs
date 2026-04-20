using MotionControl.Control.Homing;
using MotionControl.Control.Interfaces;
using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class HomingService(
    IMotionController motionController,
    IEnumerable<IHomeStrategy> homeStrategies,
    HomePlanRuntimeState homePlanRuntimeState,
    CommandFeedbackRuntimeState commandFeedbackRuntimeState,
    AxisStateMachine axisStateMachine) : IHomingService
{
    public async Task HomeAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var strategy = homeStrategies.FirstOrDefault(item => item.HomeMode == axis.HomeMode)
            ?? homeStrategies.First(item => item.HomeMode == MotionControl.Domain.Enums.HomeMode.Default);

        var plan = strategy.BuildPlan(axis);
        homePlanRuntimeState.Update(plan);

        axis.ApplyState(axisStateMachine.OnHomeIssued());
        var result = await motionController.HomeAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Home", AxisNo = axis.ControllerAxisNo, Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
            throw new InvalidOperationException($"Home axis failed: {result.ErrorMessage}");
        }

        await strategy.ExecuteAsync(axis, cancellationToken);
        axis.ApplyState(axisStateMachine.OnHomeSucceeded(axis));
        commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Home", AxisNo = axis.ControllerAxisNo, Status = "Succeeded", Message = strategy.BuildPlan(axis).Title });
    }
}
