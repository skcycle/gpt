using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.StateMachines;

public sealed class AxisStateMachine
{
    public AxisState GetNextState(Axis axis)
    {
        if (axis.HasAlarm)
        {
            return AxisState.Alarm;
        }

        if (axis.ServoState == ServoState.Off)
        {
            return AxisState.Disabled;
        }

        if (Math.Abs(axis.CurrentVelocity) > 0.001d)
        {
            return AxisState.Moving;
        }

        if (!axis.IsHomed && axis.ServoState == ServoState.On)
        {
            return AxisState.Standstill;
        }

        return AxisState.Standstill;
    }
}
