namespace MotionControl.Control.Homing;

public sealed class HomeExecutionPlan
{
    public IReadOnlyList<string> Steps { get; init; } = Array.Empty<string>();
}
