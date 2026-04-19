namespace MotionControl.Domain.Enums;

public enum SystemState
{
    PowerOff = 0,
    Initializing = 1,
    Idle = 2,
    Ready = 3,
    Manual = 4,
    Auto = 5,
    Warning = 6,
    Alarm = 7,
    EmergencyStop = 8
}
