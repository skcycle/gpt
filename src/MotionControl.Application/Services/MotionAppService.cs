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

    private Axis FindAxis(int axisNo)
    {
        return machine.Axes.FirstOrDefault(a => a.ControllerAxisNo == axisNo)
               ?? throw new InvalidOperationException($"Axis {axisNo} not found.");
    }
}
