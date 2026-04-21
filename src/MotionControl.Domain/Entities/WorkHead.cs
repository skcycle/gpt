namespace MotionControl.Domain.Entities;

public sealed class WorkHead
{
    public WorkHead(string name, string description, int xAxisNo, int yAxisNo, int zAxisNo, int rAxisNo, int vacuumOutputAddress, int blowOutputAddress, int vacuumInputAddress)
    {
        Name = name;
        Description = description;
        XAxisNo = xAxisNo;
        YAxisNo = yAxisNo;
        ZAxisNo = zAxisNo;
        RAxisNo = rAxisNo;
        VacuumOutputAddress = vacuumOutputAddress;
        BlowOutputAddress = blowOutputAddress;
        VacuumInputAddress = vacuumInputAddress;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public int XAxisNo { get; private set; }
    public int YAxisNo { get; private set; }
    public int ZAxisNo { get; private set; }
    public int RAxisNo { get; private set; }
    public int VacuumOutputAddress { get; private set; }
    public int BlowOutputAddress { get; private set; }
    public int VacuumInputAddress { get; private set; }

    public void UpdateMetadata(string name, string description, int xAxisNo, int yAxisNo, int zAxisNo, int rAxisNo, int vacuumOutputAddress, int blowOutputAddress, int vacuumInputAddress)
    {
        Name = name;
        Description = description;
        XAxisNo = xAxisNo;
        YAxisNo = yAxisNo;
        ZAxisNo = zAxisNo;
        RAxisNo = rAxisNo;
        VacuumOutputAddress = vacuumOutputAddress;
        BlowOutputAddress = blowOutputAddress;
        VacuumInputAddress = vacuumInputAddress;
    }
}
