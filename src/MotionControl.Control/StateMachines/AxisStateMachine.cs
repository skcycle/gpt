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

        if (!axis.IsHomed && axis.ServoState == ServoState.On)
        {
            return AxisState.Standstill;
        }

        return axis.State;
    }
}
