using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class AxisPollingService(
    IAxisMotionController motionController,
    Machine machine,
    AxisStateMachine axisStateMachine,
    CommandFeedbackRuntimeState commandFeedbackRuntimeState)
{
    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        foreach (var axis in machine.Axes)
        {
            var feedback = await motionController.GetAxisFeedbackAsync(axis.ControllerAxisNo, cancellationToken);
            axis.UpdateFeedback(
                feedback.CommandPosition,
                feedback.EncoderPosition,
                feedback.CurrentVelocity,
                feedback.AxisState,
                feedback.ServoState,
                feedback.HasAlarm,
                feedback.IsHomed,
                feedback.PositiveHardLimitTriggered,
                feedback.NegativeHardLimitTriggered,
                feedback.PositiveSoftLimitTriggered,
                feedback.NegativeSoftLimitTriggered);

            var previousState = axis.State;
            var nextState = axisStateMachine.GetNextState(axis);
            if (previousState != nextState)
            {
                axis.ApplyState(nextState);
                commandFeedbackRuntimeState.Add(new CommandFeedback
                {
                    CommandName = "AxisState",
                    AxisNo = axis.ControllerAxisNo,
                    Status = "Changed",
                    Message = $"{previousState} -> {nextState}"
                });
            }
        }
    }
}
