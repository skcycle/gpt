namespace MotionControl.Domain.Enums;

public enum SystemState
{
    PowerOff = 0,
    Initializing = 1,
    Idle = 2,
    Standby = 3,
    Ready = 4,
    Manual = 5,
    Auto = 6,
    Warning = 7,
    Alarm = 8,
    Fault = 9,
    EmergencyStop = 10,
    FaultRecovering = 11
}
