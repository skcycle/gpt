using MotionControl.Application.DTOs;
using MotionControl.Application.Interfaces;
using MotionControl.Control.Interfaces;
using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;

namespace MotionControl.Application.Services;

public sealed class MotionAppService(
    IAxisControlService axisControlService,
    IHomingService homingService,
    Machine machine) : IMotionAppService
{
    public async Task EnableAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        await axisControlService.EnableAxisAsync(axis, cancellationToken);
    }

    public async Task HomeAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        await homingService.HomeAxisAsync(axis, cancellationToken);
    }

    public async Task MoveAbsoluteAsync(MoveAxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        await axisControlService.MoveAbsoluteAsync(axis, command.Position, command.Velocity, command.Acceleration, command.Deceleration, cancellationToken);
    }

    public async Task StopAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        await axisControlService.StopAsync(axis, cancellationToken);
    }

    public async Task JogAxisAsync(JogAxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        var direction = command.PositiveDirection ? 1 : -1;
        var nextPosition = axis.CurrentPosition + direction * 10.0;
        axis.SetTargetPosition(nextPosition, MotionControl.Domain.Enums.MotionMode.Jog);
        await axisControlService.MoveAbsoluteAsync(axis, nextPosition, command.Velocity, command.Velocity, command.Velocity, cancellationToken);
    }

    private Axis FindAxis(int axisNo)
    {
        return machine.Axes.FirstOrDefault(a => a.Id.Value == axisNo)
               ?? throw new InvalidOperationException($"Axis {axisNo} not found.");
    }
}
