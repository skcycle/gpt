namespace MotionControl.Device.Abstractions.Results;

public sealed record DeviceResult(bool Success, string? ErrorMessage = null)
{
    public static DeviceResult Ok() => new(true);
    public static DeviceResult Fail(string errorMessage) => new(false, errorMessage);
}
