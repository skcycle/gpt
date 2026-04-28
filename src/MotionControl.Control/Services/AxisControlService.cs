using MotionControl.Control.Interfaces;
using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Models;
using MotionControl.Device.Abstractions.Results;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.Services;

public sealed class AxisControlService(
    IAxisMotionController motionController,
    CommandFeedbackRuntimeState commandFeedbackRuntimeState,
    AxisStateMachine axisStateMachine) : IAxisControlService
{
    public async Task<bool> IsServoOnAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var feedback = await motionController.GetAxisFeedbackAsync(axis.ControllerAxisNo, cancellationToken);
        return feedback.ServoState == ServoState.On;
    }

    public async Task<DeviceResult> EnableAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        commandFeedbackRuntimeState.AddStarted("Enable", axis.ControllerAxisNo, "Axis enable requested");
        var result = await motionController.EnableAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.AddFailed("Enable", axis.ControllerAxisNo, result.ErrorMessage ?? "Unknown error");
            return DeviceResult.Fail($"轴 {axis.Id.Value} ({axis.Name}) 使能失败: {result.ErrorMessage}");
        }

        axis.ApplyState(axisStateMachine.OnEnableSucceeded(axis));
        commandFeedbackRuntimeState.AddSucceeded("Enable", axis.ControllerAxisNo, "Axis enabled");
        return DeviceResult.Ok();
    }

    public async Task<DeviceResult> DisableAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        commandFeedbackRuntimeState.AddStarted("Disable", axis.ControllerAxisNo, "Axis disable requested");
        var result = await motionController.DisableAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.AddFailed("Disable", axis.ControllerAxisNo, result.ErrorMessage ?? "Unknown error");
            return DeviceResult.Fail($"轴 {axis.Id.Value} ({axis.Name}) 失能失败: {result.ErrorMessage}");
        }

        axis.ApplyState(axisStateMachine.OnDisableSucceeded());
        commandFeedbackRuntimeState.AddSucceeded("Disable", axis.ControllerAxisNo, "Axis disabled");
        return DeviceResult.Ok();
    }

    public async Task<DeviceResult> MoveAbsoluteAsync(Axis axis, double position, double velocity, double acceleration, double deceleration, CancellationToken cancellationToken = default)
    {
        var command = new AxisMoveCommand(position, velocity, acceleration, deceleration);
        commandFeedbackRuntimeState.AddStarted("Move", axis.ControllerAxisNo, $"Target {position}");
        var result = await motionController.MoveAbsoluteAsync(axis.ControllerAxisNo, command, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.AddFailed("Move", axis.ControllerAxisNo, result.ErrorMessage ?? "Unknown error");
            return DeviceResult.Fail($"轴 {axis.Id.Value} ({axis.Name}) 定位失败: {result.ErrorMessage}");
        }

        axis.SetTargetPosition(position, MotionMode.Absolute);
        axis.ApplyState(axisStateMachine.OnMoveIssued());
        commandFeedbackRuntimeState.AddSucceeded("Move", axis.ControllerAxisNo, $"Target {position}");
        return DeviceResult.Ok();
    }

    public async Task<DeviceResult> JogAsync(Axis axis, double velocity, bool positiveDirection, CancellationToken cancellationToken = default)
    {
        commandFeedbackRuntimeState.AddStarted("Jog", axis.ControllerAxisNo, positiveDirection ? "Jog positive requested" : "Jog negative requested");
        var result = await motionController.JogAxisAsync(axis.ControllerAxisNo, velocity, positiveDirection, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.AddFailed("Jog", axis.Id.Value, result.ErrorMessage ?? "Unknown error");
            return DeviceResult.Fail($"轴 {axis.Id.Value} ({axis.Name}) Jog 失败: {result.ErrorMessage}");
        }

        axis.SetTargetPosition(axis.CurrentPosition, MotionMode.Jog);
        axis.ApplyState(axisStateMachine.OnJogIssued());
        commandFeedbackRuntimeState.AddSucceeded("Jog", axis.Id.Value, positiveDirection ? "Jog positive" : "Jog negative");
        return DeviceResult.Ok();
    }

    public async Task<DeviceResult> StopAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        axis.ApplyState(axisStateMachine.OnStopIssued());
        commandFeedbackRuntimeState.AddStarted("Stop", axis.ControllerAxisNo, "Axis stop requested");
        var result = await motionController.StopAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.AddFailed("Stop", axis.ControllerAxisNo, result.ErrorMessage ?? "Unknown error");
            return DeviceResult.Fail($"轴 {axis.Id.Value} ({axis.Name}) 停止失败: {result.ErrorMessage}");
        }

        axis.ApplyState(axisStateMachine.OnStopSucceeded(axis));
        commandFeedbackRuntimeState.AddSucceeded("Stop", axis.ControllerAxisNo, "Axis stopped");
        return DeviceResult.Ok();
    }

    public async Task<DeviceResult> ResetAlarmAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        commandFeedbackRuntimeState.AddStarted("ResetAlarm", axis.ControllerAxisNo, "Axis alarm reset requested");
        var result = await motionController.ResetAxisAlarmAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            commandFeedbackRuntimeState.AddFailed("ResetAlarm", axis.ControllerAxisNo, result.ErrorMessage ?? "Unknown error");
            return DeviceResult.Fail($"轴 {axis.Id.Value} ({axis.Name}) 报警复位失败: {result.ErrorMessage}");
        }

        axis.ClearAlarm();
        axis.ApplyState(axisStateMachine.OnAlarmResetSucceeded(axis));
        commandFeedbackRuntimeState.AddSucceeded("ResetAlarm", axis.ControllerAxisNo, "Alarm reset");
        return DeviceResult.Ok();
    }
}
