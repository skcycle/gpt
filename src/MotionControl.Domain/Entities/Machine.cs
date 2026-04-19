using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Entities;

public sealed class Machine
{
    public Machine(
        IReadOnlyCollection<Axis> axes,
        IReadOnlyCollection<AxisGroup> groups,
        IReadOnlyCollection<IoPoint>? ioPoints = null,
        IReadOnlyCollection<Alarm>? alarms = null)
    {
        Axes = axes;
        Groups = groups;
        IoPoints = ioPoints ?? Array.Empty<IoPoint>();
        Alarms = alarms ?? Array.Empty<Alarm>();
    }

    public IReadOnlyCollection<Axis> Axes { get; }
    public IReadOnlyCollection<AxisGroup> Groups { get; }
    public IReadOnlyCollection<IoPoint> IoPoints { get; }
    public IReadOnlyCollection<Alarm> Alarms { get; }
    public SystemState CurrentState { get; private set; } = SystemState.Initializing;

    public void SetSystemState(SystemState state) => CurrentState = state;
}
