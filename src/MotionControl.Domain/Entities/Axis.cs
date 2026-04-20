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
    public double EncoderPosition { get; private set; }
    public double CurrentVelocity { get; private set; }
    public double TargetPosition { get; private set; }
    public bool IsHomed { get; private set; }
    public bool HasAlarm { get; private set; }
    public bool PositiveLimitTriggered { get; private set; }
    public bool NegativeLimitTriggered { get; private set; }
    public bool PositiveSoftLimitTriggered { get; private set; }
    public bool NegativeSoftLimitTriggered { get; private set; }
    public SoftLimit? SoftLimit { get; private set; }
    public double WorkVelocity { get; private set; } = 200;
    public double SetupVelocity { get; private set; } = 50;
    public double PulseEquivalent { get; private set; } = 1000;
    public HomeMode HomeMode { get; private set; } = HomeMode.Default;
    public string ServoBinding { get; private set; } = string.Empty;

    public void UpdateFeedback(
        double currentPosition,
        double encoderPosition,
        double currentVelocity,
        AxisState state,
        ServoState servoState,
        bool hasAlarm,
        bool isHomed,
        bool positiveLimitTriggered,
        bool negativeLimitTriggered,
        bool positiveSoftLimitTriggered,
        bool negativeSoftLimitTriggered)
    {
        CurrentPosition = currentPosition;
        EncoderPosition = encoderPosition;
        CurrentVelocity = currentVelocity;
        State = state;
        ServoState = servoState;
        HasAlarm = hasAlarm;
        IsHomed = isHomed;
        PositiveLimitTriggered = positiveLimitTriggered;
        NegativeLimitTriggered = negativeLimitTriggered;
        PositiveSoftLimitTriggered = positiveSoftLimitTriggered;
        NegativeSoftLimitTriggered = negativeSoftLimitTriggered;
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
    public void SetWorkVelocity(double workVelocity) => WorkVelocity = workVelocity;
    public void SetSetupVelocity(double setupVelocity) => SetupVelocity = setupVelocity;
    public void SetPulseEquivalent(double pulseEquivalent) => PulseEquivalent = pulseEquivalent <= 0 ? 1000 : pulseEquivalent;
    public void SetHomeMode(HomeMode homeMode) => HomeMode = homeMode;
    public void SetServoBinding(string servoBinding) => ServoBinding = servoBinding;
}
