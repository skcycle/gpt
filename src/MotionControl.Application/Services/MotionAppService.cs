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

    public async Task DisableAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        await axisControlService.DisableAxisAsync(axis, cancellationToken);
    }

    public async Task HomeAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        await homingService.HomeAxisAsync(axis, cancellationToken);
    }

    public async Task MoveAbsoluteAsync(MoveAxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        var pulseEquivalent = axis.PulseEquivalent <= 0 ? 1000 : axis.PulseEquivalent;
        var pulsePosition = command.Position * pulseEquivalent;
        var velocity = command.Velocity > 0 ? command.Velocity : axis.WorkVelocity;
        await axisControlService.MoveAbsoluteAsync(axis, pulsePosition, velocity, command.Acceleration, command.Deceleration, cancellationToken);
    }

    public async Task StopAxisAsync(AxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        await axisControlService.StopAsync(axis, cancellationToken);
    }

    public async Task JogAxisAsync(JogAxisCommandDto command, CancellationToken cancellationToken = default)
    {
        var axis = FindAxis(command.AxisNo);
        var velocity = command.Velocity > 0 ? command.Velocity : axis.SetupVelocity;
        await axisControlService.JogAsync(axis, velocity, command.PositiveDirection, cancellationToken);
    }

    private Axis FindAxis(int axisNo)
    {
        return machine.Axes.FirstOrDefault(a => a.Id.Value == axisNo)
               ?? throw new InvalidOperationException($"Axis {axisNo} not found.");
    }
}
