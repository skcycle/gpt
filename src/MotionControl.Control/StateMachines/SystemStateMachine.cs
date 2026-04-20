using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.StateMachines;

public sealed class SystemStateMachine
{
    public SystemState OnInitializeRequested() => SystemState.Initializing;

    public SystemState OnRecoveryStarted() => SystemState.FaultRecovering;

    public SystemState OnRecoveryCompleted() => SystemState.Standby;

    public SystemState OnPolling(Machine machine, EtherCatControllerStatus controllerStatus)
        => GetNextState(machine, controllerStatus);

    public SystemState GetNextState(Machine machine, EtherCatControllerStatus controllerStatus)
    {
        var hasAxisAlarm = machine.Axes.Any(axis => axis.HasAlarm);
        var hasSystemAlarm = machine.Alarms.Any(alarm => alarm.IsActive);
        var hasSlaveAlarm = controllerStatus.Slaves.Any(slave => slave.HasAlarm);
        var hasAnyAlarm = hasAxisAlarm || hasSystemAlarm || hasSlaveAlarm;
        var anyAxisHoming = machine.Axes.Any(axis => axis.State == AxisState.Homing);
        var anyAxisMoving = machine.Axes.Any(axis => axis.State == AxisState.Moving);
        var anyServoOn = machine.Axes.Any(axis => axis.ServoState == ServoState.On);

        if (machine.CurrentState == SystemState.FaultRecovering)
        {
            if (!controllerStatus.IsConnected)
            {
                return SystemState.FaultRecovering;
            }

            return hasAnyAlarm ? SystemState.FaultRecovering : SystemState.Standby;
        }

        if (!controllerStatus.IsConnected)
        {
            return SystemState.Fault;
        }

        if (hasAnyAlarm)
        {
            return SystemState.Alarm;
        }

        if (!controllerStatus.IsOperational)
        {
            return SystemState.Initializing;
        }

        if (anyAxisHoming || anyAxisMoving)
        {
            return SystemState.Manual;
        }

        if (anyServoOn)
        {
            return SystemState.Ready;
        }

        return SystemState.Standby;
    }
}
