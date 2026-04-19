namespace MotionControl.Infrastructure.Configuration;

public sealed class AxisMappingItem
{
    public int AxisNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public bool IsMaster { get; set; }
    public string? MasterAxisName { get; set; }
    public double? SoftLimitPositive { get; set; }
    public double? SoftLimitNegative { get; set; }
    public string HomeMode { get; set; } = "Default";
    public string ServoBinding { get; set; } = string.Empty;
}
