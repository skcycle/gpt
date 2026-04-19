using MotionControl.Domain.Entities;

namespace MotionControl.Diagnostics.Services;

public sealed class SafetyInterlockService
{
    public bool CanEnableAxis(Axis axis) => !axis.HasAlarm;
    public bool CanStartMove(Axis axis) => !axis.HasAlarm && axis.IsHomed;
}
