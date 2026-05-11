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

    public SystemState OnEmergencyStopRequested() => SystemState.EmergencyStop;

    public SystemState OnEmergencyStopCleared(Machine machine, EtherCatControllerStatus? controllerStatus)
    {
        // 直接返回目标状态，不再调用 GetNextState。
        // GetNextState 的 guard (CurrentState == EmergencyStop -> return EmergencyStop)
        // 会阻止从 EmergencyStop 出去的转换，导致 CLR-ESTOP 无效。
        if (controllerStatus is null || !controllerStatus.IsConnected)
        {
            return SystemState.FaultRecovering;
        }

        // controller 已连接时，转换到 Standby（由 OnPolling 的下一次调用进一步评估）
        return SystemState.Standby;
    }

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
        if (machine.CurrentState == SystemState.EmergencyStop)
        {
            return SystemState.EmergencyStop;
        }

        var hasAxisAlarm = machine.Axes.Any(axis => axis.HasAlarm);
        var hasSystemAlarm = machine.Alarms.Any(alarm => alarm.IsActive);
        var hasSlaveAlarm = controllerStatus.HasAnySlaveAlarm;
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

        if (controllerStatus.HasOfflineSlave)
        {
            return SystemState.Warning;
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
