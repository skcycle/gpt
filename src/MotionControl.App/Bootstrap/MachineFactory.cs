using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.App.Bootstrap;

public static class MachineFactory
{
    public static Machine CreateDefaultMachine(AxisMappingOptions axisMappingOptions)
    {
        var axisNames = axisMappingOptions.AxisNames;
        var axisMappings = axisMappingOptions.Axes;
        var axes = Enumerable.Range(1, 32)
            .Select(axisNo =>
            {
                var mapping = axisMappings.FirstOrDefault(item => item.AxisNo == axisNo);
                var axis = new Axis(
                    new AxisId(axisNo),
                    mapping?.Name ?? (axisNames.Count >= axisNo ? axisNames[axisNo - 1] : $"Axis {axisNo}"),
                    axisNo);

                if (mapping?.SoftLimitNegative is not null && mapping.SoftLimitPositive is not null)
                {
                    axis.SetSoftLimit(new SoftLimit(mapping.SoftLimitNegative.Value, mapping.SoftLimitPositive.Value));
                }

                if (mapping is not null)
                {
                    axis.SetHomeMode(mapping.HomeMode);
                    axis.SetServoBinding(mapping.ServoBinding);
                }

                return axis;
            })
            .ToArray();

        var ioPoints = Enumerable.Range(0, 16)
            .Select(index => new IoPoint($"DI_{index}", index, false))
            .Concat(Enumerable.Range(0, 16).Select(index => new IoPoint($"DO_{index}", index, true)))
            .ToArray();

        return new Machine(axes, Array.Empty<AxisGroup>(), ioPoints, Array.Empty<Alarm>());
    }
}
