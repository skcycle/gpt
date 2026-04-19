using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Entities;

public sealed class Machine
{
    public Machine(IReadOnlyCollection<Axis> axes, IReadOnlyCollection<AxisGroup> groups)
    {
        Axes = axes;
        Groups = groups;
    }

    public IReadOnlyCollection<Axis> Axes { get; }
    public IReadOnlyCollection<AxisGroup> Groups { get; }
    public SystemState CurrentState { get; private set; } = SystemState.Initializing;

    public void SetSystemState(SystemState state) => CurrentState = state;
}
