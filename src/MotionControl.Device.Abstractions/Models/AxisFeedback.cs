using MotionControl.Domain.Enums;

namespace MotionControl.Device.Abstractions.Models;

public sealed record AxisFeedback(
    int AxisNo,
    double CurrentPosition,
    double CurrentVelocity,
    AxisState AxisState,
    ServoState ServoState,
    bool HasAlarm,
    bool PositiveLimitTriggered,
    bool NegativeLimitTriggered);
