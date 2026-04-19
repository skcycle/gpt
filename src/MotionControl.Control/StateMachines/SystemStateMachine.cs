using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.StateMachines;

public sealed class SystemStateMachine
{
    public SystemState GetNextState(Machine machine, EtherCatControllerStatus controllerStatus)
    {
        if (!controllerStatus.IsConnected)
        {
            return SystemState.Fault;
        }

        if (machine.Axes.Any(axis => axis.HasAlarm) || machine.Alarms.Any(alarm => alarm.IsActive))
        {
            return SystemState.Alarm;
        }

        if (!controllerStatus.IsOperational)
        {
            return SystemState.Initializing;
        }

        if (machine.Axes.Any(axis => axis.ServoState == ServoState.On))
        {
            return SystemState.Ready;
        }

        return SystemState.Standby;
    }
}
