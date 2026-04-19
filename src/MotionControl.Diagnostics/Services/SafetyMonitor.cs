namespace MotionControl.Diagnostics.Services;

public sealed class SafetyMonitor
{
    public bool CanEnableAxis() => true;
    public bool CanStartMotion() => true;
}
