namespace MotionControl.Infrastructure.Configuration;

public static class MagazinePositionKinds
{
    public const string PickStart = "PickStart";
    public const string InspectStart = "InspectStart";
    public const string Normal = "Normal";
}

public sealed class MagazinePositionConfigItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = MagazinePositionKinds.Normal;
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
