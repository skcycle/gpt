using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;

namespace MotionControl.App.Bootstrap;

public static class MachineFactory
{
    public static Machine CreateDefaultMachine()
    {
        var axes = Enumerable.Range(1, 32)
            .Select(axisNo => new Axis(new AxisId(axisNo), $"Axis {axisNo}", axisNo))
            .ToArray();

        var ioPoints = Enumerable.Range(0, 16)
            .Select(index => new IoPoint($"DI_{index}", index, false))
            .Concat(Enumerable.Range(0, 16).Select(index => new IoPoint($"DO_{index}", index, true)))
            .ToArray();

        return new Machine(axes, Array.Empty<AxisGroup>(), ioPoints, Array.Empty<Alarm>());
    }
}
