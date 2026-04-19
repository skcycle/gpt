using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.App.Bootstrap;

public static class MachineFactory
{
    public static Machine CreateDefaultMachine(AxisMappingOptions axisMappingOptions)
    {
        var axisNames = axisMappingOptions.AxisNames;
        var axes = Enumerable.Range(1, 32)
            .Select(axisNo => new Axis(
                new AxisId(axisNo),
                axisNames.Count >= axisNo ? axisNames[axisNo - 1] : $"Axis {axisNo}",
                axisNo))
            .ToArray();

        var ioPoints = Enumerable.Range(0, 16)
            .Select(index => new IoPoint($"DI_{index}", index, false))
            .Concat(Enumerable.Range(0, 16).Select(index => new IoPoint($"DO_{index}", index, true)))
            .ToArray();

        return new Machine(axes, Array.Empty<AxisGroup>(), ioPoints, Array.Empty<Alarm>());
    }
}
