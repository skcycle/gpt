namespace MotionControl.Device.Abstractions.Models;

public sealed class EtherCatControllerStatus
{
    public bool IsConnected { get; init; }
    public bool IsOperational { get; init; }
    public int OnlineSlaveCount { get; init; }
    public string NetworkState { get; init; } = "Unknown";
    public string ControllerModel { get; init; } = "ZMC432EtherCAT";
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
