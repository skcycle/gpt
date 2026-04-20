using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class AxisPollingService(
    IMotionController motionController,
    Machine machine,
    AxisStateMachine axisStateMachine)
{
    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        foreach (var axis in machine.Axes)
        {
            var feedback = await motionController.GetAxisFeedbackAsync(axis.ControllerAxisNo, cancellationToken);
            axis.UpdateFeedback(
                feedback.CurrentPosition,
                feedback.CurrentVelocity,
                feedback.AxisState,
                feedback.ServoState,
                feedback.HasAlarm,
                feedback.IsHomed,
                feedback.PositiveHardLimitTriggered,
                feedback.NegativeHardLimitTriggered,
                feedback.PositiveSoftLimitTriggered,
                feedback.NegativeSoftLimitTriggered);

            var nextState = axisStateMachine.GetNextState(axis);
            axis.ApplyState(nextState);
        }
    }
}
