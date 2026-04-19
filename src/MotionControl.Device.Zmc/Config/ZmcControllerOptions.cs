namespace MotionControl.Device.Zmc.Config;

public sealed class ZmcControllerOptions
{
    public string IpAddress { get; set; } = "127.0.0.1";
    public int AxisCount { get; set; } = 32;
    public int PollingIntervalMs { get; set; } = 50;
}
