using MotionControl.Domain.Entities;

namespace MotionControl.Diagnostics.Services;

public sealed class SafetyInterlockService
{
    /// <summary>轴有报警时禁止使能。</summary>
    public bool CanEnableAxis(Axis axis) => !axis.HasAlarm;

    /// <summary>轴有报警或未完成回零时禁止运动。</summary>
    public bool CanStartMove(Axis axis) => !axis.HasAlarm && axis.IsHomed;

    /// <summary>轴有报警时禁止回零。</summary>
    public bool CanStartHome(Axis axis) => !axis.HasAlarm;
}
