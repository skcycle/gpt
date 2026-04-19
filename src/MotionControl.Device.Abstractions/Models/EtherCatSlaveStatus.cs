namespace MotionControl.Device.Abstractions.Models;

public sealed class EtherCatSlaveStatus
{
    public int SlaveNo { get; init; }
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = "Unknown";
    public bool IsOnline { get; init; }
    public bool HasAlarm { get; init; }
}
