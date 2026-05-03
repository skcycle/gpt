using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Entities;

public enum CylinderCommandType
{
    None,
    Extend,
    Retract
}

public sealed class Cylinder
{
    public Cylinder(string name, int extendSensorInputAddress, int retractSensorInputAddress, int extendOutputAddress, int retractOutputAddress, string description = "", int actionTimeoutMs = 3000)
    {
        Name = name;
        ExtendSensorInputAddress = extendSensorInputAddress;
        RetractSensorInputAddress = retractSensorInputAddress;
        ExtendOutputAddress = extendOutputAddress;
        RetractOutputAddress = retractOutputAddress;
        Description = description;
        ActionTimeoutMs = actionTimeoutMs;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public int ExtendSensorInputAddress { get; private set; }
    public int RetractSensorInputAddress { get; private set; }
    public int ExtendOutputAddress { get; private set; }
    public int RetractOutputAddress { get; private set; }
    public int ActionTimeoutMs { get; private set; }
    public CylinderState State { get; private set; } = CylinderState.Unknown;
    public CylinderCommandType PendingCommand { get; private set; } = CylinderCommandType.None;
    public DateTime? LastCommandStartedAtUtc { get; private set; }

    public void UpdateMetadata(string name, int extendSensorInputAddress, int retractSensorInputAddress, int extendOutputAddress, int retractOutputAddress, string description, int actionTimeoutMs = 3000)
    {
        Name = name;
        ExtendSensorInputAddress = extendSensorInputAddress;
        RetractSensorInputAddress = retractSensorInputAddress;
        ExtendOutputAddress = extendOutputAddress;
        RetractOutputAddress = retractOutputAddress;
        Description = description;
        ActionTimeoutMs = actionTimeoutMs;
    }

    public void UpdateState(bool extendSensorOn, bool retractSensorOn, bool extendOutputOn, bool retractOutputOn)
    {
        var hasExtendSensor = ExtendSensorInputAddress >= 0;
        var hasRetractSensor = RetractSensorInputAddress >= 0;

        // 双控气缸：先收紧异常状态判定
        if (hasExtendSensor && hasRetractSensor)
        {
            if (extendSensorOn && retractSensorOn)
            {
                State = CylinderState.Conflict;
                return;
            }

            if (extendOutputOn && retractOutputOn)
            {
                State = CylinderState.Conflict;
                return;
            }

            if (!extendOutputOn && !retractOutputOn && (extendSensorOn || retractSensorOn))
            {
                State = CylinderState.Conflict;
                return;
            }

            if (extendSensorOn)
            {
                State = CylinderState.Extended;
                if (PendingCommand == CylinderCommandType.Extend) ClearPendingCommand();
                return;
            }

            if (retractSensorOn)
            {
                State = CylinderState.Retracted;
                if (PendingCommand == CylinderCommandType.Retract) ClearPendingCommand();
                return;
            }

            if (extendOutputOn)
            {
                State = CylinderState.Extending;
                return;
            }

            if (retractOutputOn)
            {
                State = CylinderState.Retracting;
                return;
            }

            State = CylinderState.Unknown;
            return;
        }

        // 单作用伸侧：伸侧传感器亮但输出未驱动，按异常处理
        if (hasExtendSensor && !hasRetractSensor)
        {
            if (extendSensorOn && !extendOutputOn)
            {
                State = CylinderState.Conflict;
                return;
            }

            if (extendSensorOn)
            {
                State = CylinderState.Extended;
                if (PendingCommand == CylinderCommandType.Extend) ClearPendingCommand();
                return;
            }

            if (extendOutputOn)
            {
                State = CylinderState.Extending;
                return;
            }

            State = CylinderState.Retracted;
            if (PendingCommand == CylinderCommandType.Extend) ClearPendingCommand();
            return;
        }

        // 单作用缩侧：缩侧传感器亮但输出未驱动，按异常处理
        if (!hasExtendSensor && hasRetractSensor)
        {
            if (retractSensorOn && !retractOutputOn)
            {
                State = CylinderState.Conflict;
                return;
            }

            if (retractSensorOn)
            {
                State = CylinderState.Retracted;
                if (PendingCommand == CylinderCommandType.Retract) ClearPendingCommand();
                return;
            }

            if (retractOutputOn)
            {
                State = CylinderState.Retracting;
                return;
            }

            State = CylinderState.Extended;
            if (PendingCommand == CylinderCommandType.Retract) ClearPendingCommand();
            return;
        }

        State = CylinderState.Unknown;
    }

    public void StartExtendCommand()
    {
        PendingCommand = CylinderCommandType.Extend;
        LastCommandStartedAtUtc = DateTime.UtcNow;
    }

    public void StartRetractCommand()
    {
        PendingCommand = CylinderCommandType.Retract;
        LastCommandStartedAtUtc = DateTime.UtcNow;
    }

    public void ClearPendingCommand()
    {
        PendingCommand = CylinderCommandType.None;
        LastCommandStartedAtUtc = null;
    }

    public bool IsActionTimedOut(DateTime utcNow)
    {
        if (PendingCommand == CylinderCommandType.None || LastCommandStartedAtUtc is null)
        {
            return false;
        }

        return utcNow - LastCommandStartedAtUtc.Value >= TimeSpan.FromMilliseconds(ActionTimeoutMs);
    }
}
