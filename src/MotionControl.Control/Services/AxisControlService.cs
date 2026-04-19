using MotionControl.Control.Interfaces;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.Services;

public sealed class AxisControlService(IMotionController motionController) : IAxisControlService
{
    public async Task EnableAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var result = await motionController.EnableAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Enable axis failed: {result.ErrorMessage}");
        }
    }

    public async Task DisableAxisAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var result = await motionController.DisableAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Disable axis failed: {result.ErrorMessage}");
        }
    }

    public async Task MoveAbsoluteAsync(Axis axis, double position, double velocity, double acceleration, double deceleration, CancellationToken cancellationToken = default)
    {
        var command = new AxisMoveCommand(position, velocity, acceleration, deceleration);
        var result = await motionController.MoveAbsoluteAsync(axis.ControllerAxisNo, command, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Move absolute failed: {result.ErrorMessage}");
        }

        axis.SetTargetPosition(position, MotionMode.Absolute);
    }

    public async Task StopAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var result = await motionController.StopAxisAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Stop axis failed: {result.ErrorMessage}");
        }
    }

    public async Task ResetAlarmAsync(Axis axis, CancellationToken cancellationToken = default)
    {
        var result = await motionController.ResetAxisAlarmAsync(axis.ControllerAxisNo, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Reset alarm failed: {result.ErrorMessage}");
        }

        axis.ClearAlarm();
    }
}
