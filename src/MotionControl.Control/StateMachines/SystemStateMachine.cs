using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.StateMachines;

public sealed class SystemStateMachine
{
    public SystemState OnInitializeRequested() => SystemState.Initializing;

    public SystemState OnConnectingRequested() => SystemState.Connecting;

    public SystemState OnSyncingRequested() => SystemState.Syncing;

    public SystemState OnRecoveryStarted() => SystemState.FaultRecovering;

    public SystemState OnRecoveryCompleted(Machine machine, EtherCatControllerStatus? controllerStatus)
    {
        if (controllerStatus is null)
        {
            return SystemState.FaultRecovering;
        }

        return OnPolling(machine, controllerStatus);
    }

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
            return machine.CurrentState is SystemState.Initializing or SystemState.Connecting
                ? SystemState.Connecting
                : SystemState.Fault;
        }

        if (!controllerStatus.IsOperational)
        {
            return SystemState.Syncing;
        }

        if (hasAnyAlarm)
        {
            return SystemState.Alarm;
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
