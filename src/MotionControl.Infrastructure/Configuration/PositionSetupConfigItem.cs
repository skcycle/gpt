namespace MotionControl.Infrastructure.Configuration;

public sealed class PositionSetupConfigItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double SafeZ { get; set; }
    public int XxAxisNo { get; set; } = -1;
    public int XAxisNo { get; set; } = -1;
    public int YAxisNo { get; set; } = -1;
    public int ZAxisNo { get; set; } = -1;
    public int UAxisNo { get; set; } = -1;
    public int VAxisNo { get; set; } = -1;
    public int WAxisNo { get; set; } = -1;
    public double XxPosition { get; set; }
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public double ZPosition { get; set; }
    public double UPosition { get; set; }
    public double VPosition { get; set; }
    public double WPosition { get; set; }
}
