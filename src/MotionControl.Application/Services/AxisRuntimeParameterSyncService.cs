using MotionControl.Application.Interfaces;
using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class AxisRuntimeParameterSyncService(Machine machine) : IAxisRuntimeParameterSyncService
{
    public Task ApplyAsync(AxisMappingItem axisMappingItem, CancellationToken cancellationToken = default)
    {
        var axis = machine.Axes.FirstOrDefault(item => item.Id.Value == axisMappingItem.AxisNo);
        if (axis is null)
        {
            return Task.CompletedTask;
        }

        if (axisMappingItem.SoftLimitNegative.HasValue && axisMappingItem.SoftLimitPositive.HasValue)
        {
            axis.SetSoftLimit(new SoftLimit(axisMappingItem.SoftLimitNegative.Value, axisMappingItem.SoftLimitPositive.Value));
        }

        axis.SetHomeMode(axisMappingItem.HomeMode);
        axis.SetServoBinding(axisMappingItem.ServoBinding);
        if (axisMappingItem.WorkVelocity.HasValue) axis.SetWorkVelocity(axisMappingItem.WorkVelocity.Value);
        if (axisMappingItem.SetupVelocity.HasValue) axis.SetSetupVelocity(axisMappingItem.SetupVelocity.Value);
        if (axisMappingItem.PulseEquivalent.HasValue) axis.SetPulseEquivalent(axisMappingItem.PulseEquivalent.Value);

        return Task.CompletedTask;
    }
}
