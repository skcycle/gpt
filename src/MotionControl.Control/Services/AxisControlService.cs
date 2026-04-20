using MotionControl.Control.Interfaces;
using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.Services;

public sealed class AxisControlService(
    IMotionController motionController,
    CommandFeedbackRuntimeState commandFeedbackRuntimeState,
    AxisStateMachine axisStateMachine) : IAxisControlService
{
    public async Task EnableAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var result = await motionController.EnableAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Enable", AxisNo = axis.ControllerAxisNo, Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
            throw new InvalidOperationException($"Enable axis failed: {result.ErrorMessage}");
        }

        axis.ApplyState(axisStateMachine.OnEnableSucceeded(axis));
        commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Enable", AxisNo = axis.ControllerAxisNo, Status = "Succeeded", Message = "Axis enabled" });
    }

    public async Task DisableAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var result = await motionController.DisableAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Disable", AxisNo = axis.ControllerAxisNo, Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
            throw new InvalidOperationException($"Disable axis failed: {result.ErrorMessage}");
        }

        axis.ApplyState(axisStateMachine.OnDisableSucceeded());
        commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Disable", AxisNo = axis.ControllerAxisNo, Status = "Succeeded", Message = "Axis disabled" });
    }

    public async Task MoveAbsoluteAsync(Axis axis, double position, double velocity, double acceleration, double deceleration, CancellationToken cancellationToken = default)
    {
        var command = new AxisMoveCommand(position, velocity, acceleration, deceleration);
        var result = await motionController.MoveAbsoluteAsync(axis.ControllerAxisNo, command, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Move", AxisNo = axis.ControllerAxisNo, Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
            throw new InvalidOperationException($"Move absolute failed: {result.ErrorMessage}");
        }

        axis.SetTargetPosition(position, MotionMode.Absolute);
        axis.ApplyState(axisStateMachine.OnMoveIssued());
        commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Move", AxisNo = axis.ControllerAxisNo, Status = "Succeeded", Message = $"Target {position}" });
    }

    public async Task JogAsync(Axis axis, double velocity, bool positiveDirection, CancellationToken cancellationToken = default)
    {
        var result = await motionController.JogAxisAsync(axis.ControllerAxisNo, velocity, positiveDirection, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Jog", AxisNo = axis.Id.Value, Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
            throw new InvalidOperationException($"Jog axis failed: {result.ErrorMessage}");
        }

        axis.SetTargetPosition(axis.CurrentPosition, MotionMode.Jog);
        axis.ApplyState(axisStateMachine.OnJogIssued());
        commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Jog", AxisNo = axis.Id.Value, Status = "Succeeded", Message = positiveDirection ? "Jog positive" : "Jog negative" });
    }

    public async Task StopAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        axis.ApplyState(axisStateMachine.OnStopIssued());
        var result = await motionController.StopAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Stop", AxisNo = axis.ControllerAxisNo, Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
            throw new InvalidOperationException($"Stop axis failed: {result.ErrorMessage}");
        }

        axis.ApplyState(axisStateMachine.OnStopSucceeded(axis));
        commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "Stop", AxisNo = axis.ControllerAxisNo, Status = "Succeeded", Message = "Axis stopped" });
    }

    public async Task ResetAlarmAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var result = await motionController.ResetAxisAlarmAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "ResetAlarm", AxisNo = axis.ControllerAxisNo, Status = "Failed", Message = result.ErrorMessage ?? "Unknown error" });
            throw new InvalidOperationException($"Reset alarm failed: {result.ErrorMessage}");
        }

        axis.ClearAlarm();
        axis.ApplyState(axisStateMachine.OnAlarmResetSucceeded(axis));
        commandFeedbackRuntimeState.Add(new CommandFeedback { CommandName = "ResetAlarm", AxisNo = axis.ControllerAxisNo, Status = "Succeeded", Message = "Alarm reset" });
    }
}
