namespace MotionControl.Infrastructure.Configuration;

public sealed class WorkHeadConfigItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int XAxisNo { get; set; } = -1;
    public int YAxisNo { get; set; } = -1;
    public int ZAxisNo { get; set; } = -1;
    public int RAxisNo { get; set; } = -1;
    public int VacuumOutputAddress { get; set; } = -1;
    public int BlowOutputAddress { get; set; } = -1;
    public int VacuumInputAddress { get; set; } = -1;
    public int GeneralOutputAddress1 { get; set; } = -1;
    public int GeneralOutputAddress2 { get; set; } = -1;
    public int GeneralInputAddress1 { get; set; } = -1;
    public int GeneralInputAddress2 { get; set; } = -1;
}
