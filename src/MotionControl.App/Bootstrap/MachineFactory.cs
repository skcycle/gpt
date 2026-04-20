using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.App.Bootstrap;

public static class MachineFactory
{
    public static Machine CreateDefaultMachine(AxisMappingOptions axisMappingOptions, IReadOnlyCollection<MotionControl.Infrastructure.Configuration.IoPointConfigItem>? ioPointConfigs = null)
    {
        var axisNames = axisMappingOptions.AxisNames;
        var axisMappings = axisMappingOptions.Axes;
        var configuredAxisCount = axisMappings.Count > 0
            ? axisMappings.Max(item => item.AxisNo) + 1
            : axisNames.Count;
        var axisCount = Math.Max(1, configuredAxisCount);
        var axes = Enumerable.Range(0, axisCount)
            .Select(axisNo =>
            {
                var mapping = axisMappings.FirstOrDefault(item => item.AxisNo == axisNo);
                var axis = new Axis(
                    new AxisId(axisNo),
                    mapping?.Name ?? (axisNames.Count > axisNo ? axisNames[axisNo] : $"Axis {axisNo}"),
                    axisNo);

                if (mapping?.SoftLimitNegative is not null && mapping.SoftLimitPositive is not null)
                {
                    axis.SetSoftLimit(new SoftLimit(mapping.SoftLimitNegative.Value, mapping.SoftLimitPositive.Value));
                }

                if (mapping is not null)
                {
                    axis.SetHomeMode(mapping.HomeMode);
                    axis.SetServoBinding(mapping.ServoBinding);
                    if (mapping.WorkVelocity.HasValue) axis.SetWorkVelocity(mapping.WorkVelocity.Value);
                    if (mapping.SetupVelocity.HasValue) axis.SetSetupVelocity(mapping.SetupVelocity.Value);
                    if (mapping.PulseEquivalent.HasValue) axis.SetPulseEquivalent(mapping.PulseEquivalent.Value);
                }

                return axis;
            })
            .ToArray();

        var ioPoints = (ioPointConfigs is { Count: > 0 }
                ? ioPointConfigs.Select(item => new IoPoint(item.Name, item.Address, item.IsOutput, item.Description))
                : Enumerable.Range(0, 16)
                    .Select(index => new IoPoint($"DI_{index}", index, false))
                    .Concat(Enumerable.Range(0, 16).Select(index => new IoPoint($"DO_{index}", index, true))))
            .ToArray();

        var alarms = new[]
        {
            new Alarm("SYS-001", "EtherCAT controller not connected", DateTime.Now, "System", "Communication", "Warning"),
            new Alarm("AXIS-001", "Axis 1 follow error placeholder", DateTime.Now, "Axis 1", "Motion", "Error")
        };

        alarms[0].Clear();
        alarms[1].Clear();

        return new Machine(axes, Array.Empty<AxisGroup>(), ioPoints, alarms);
    }
}
