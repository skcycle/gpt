using MotionControl.Domain.Enums;
using MotionControl.Domain.ValueObjects;

namespace MotionControl.Domain.Entities;

public sealed class Axis
{
    public Axis(AxisId id, string name, int controllerAxisNo)
    {
        Id = id;
        Name = name;
        ControllerAxisNo = controllerAxisNo;
    }

    public AxisId Id { get; }
    public string Name { get; private set; }
    public int ControllerAxisNo { get; }
    public AxisState State { get; private set; } = AxisState.Disabled;
    public ServoState ServoState { get; private set; } = ServoState.Off;
    public MotionMode MotionMode { get; private set; } = MotionMode.None;
    public double CurrentPosition { get; private set; }
    public double CurrentVelocity { get; private set; }
    public double TargetPosition { get; private set; }
    public bool IsHomed { get; private set; }
    public bool HasAlarm { get; private set; }
    public bool PositiveLimitTriggered { get; private set; }
    public bool NegativeLimitTriggered { get; private set; }
    public SoftLimit? SoftLimit { get; private set; }

    public void UpdateFeedback(
        double currentPosition,
        double currentVelocity,
        AxisState state,
        ServoState servoState,
        bool hasAlarm,
        bool positiveLimitTriggered,
        bool negativeLimitTriggered)
    {
        CurrentPosition = currentPosition;
        CurrentVelocity = currentVelocity;
        State = state;
        ServoState = servoState;
        HasAlarm = hasAlarm;
        PositiveLimitTriggered = positiveLimitTriggered;
        NegativeLimitTriggered = negativeLimitTriggered;
    }

    public void SetTargetPosition(double targetPosition, MotionMode motionMode)
    {
        TargetPosition = targetPosition;
        MotionMode = motionMode;
    }

    public void ApplyState(AxisState state)
    {
        State = state;
    }

    public void MarkHomed() => IsHomed = true;
    public void ClearHomed() => IsHomed = false;
    public void SetAlarm() => HasAlarm = true;
    public void ClearAlarm() => HasAlarm = false;
    public void SetSoftLimit(SoftLimit softLimit) => SoftLimit = softLimit;
}
