namespace MotionControl.Domain.Entities;

public sealed class WorkHead
{
    public WorkHead(string name, string description, int xAxisNo, int yAxisNo, int zAxisNo, int rAxisNo, int vacuumOutputAddress, int blowOutputAddress, int vacuumInputAddress, int generalOutputAddress1, int generalOutputAddress2, int generalInputAddress1, int generalInputAddress2, int vacuumTimeoutMs = 3000, List<WorkHeadPosition>? positions = null)
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
        GeneralOutputAddress1 = generalOutputAddress1;
        GeneralOutputAddress2 = generalOutputAddress2;
        GeneralInputAddress1 = generalInputAddress1;
        GeneralInputAddress2 = generalInputAddress2;
        VacuumTimeoutMs = vacuumTimeoutMs;
        Positions = positions ?? new List<WorkHeadPosition>();
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
    public int GeneralOutputAddress1 { get; private set; }
    public int GeneralOutputAddress2 { get; private set; }
    public int GeneralInputAddress1 { get; private set; }
    public int GeneralInputAddress2 { get; private set; }
    public int VacuumTimeoutMs { get; private set; }

    public bool PendingVacuumCommand { get; private set; }
    public DateTime? VacuumCommandStartedAtUtc { get; private set; }
    public bool VacuumSuccessLogged { get; set; }
    public bool VacuumTimeoutLogged { get; set; }
    public bool VacuumConflictLogged { get; set; }

    public List<WorkHeadPosition> Positions { get; private set; } = new();

    public void UpdateMetadata(string name, string description, int xAxisNo, int yAxisNo, int zAxisNo, int rAxisNo, int vacuumOutputAddress, int blowOutputAddress, int vacuumInputAddress, int generalOutputAddress1, int generalOutputAddress2, int generalInputAddress1, int generalInputAddress2, int vacuumTimeoutMs)
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
        GeneralOutputAddress1 = generalOutputAddress1;
        GeneralOutputAddress2 = generalOutputAddress2;
        GeneralInputAddress1 = generalInputAddress1;
        GeneralInputAddress2 = generalInputAddress2;
        VacuumTimeoutMs = vacuumTimeoutMs;
    }

    public void AddPosition(WorkHeadPosition position)
    {
        Positions.Add(position);
    }

    public void RemovePosition(string positionName)
    {
        Positions.RemoveAll(p => p.Name == positionName);
    }

    public void UpdatePosition(string originalName, string name, string description, double x, double y, double z, double r)
    {
        var pos = Positions.FirstOrDefault(p => p.Name == originalName);
        if (pos is null) return;
        pos.Name = name;
        pos.Description = description;
        pos.X = x;
        pos.Y = y;
        pos.Z = z;
        pos.R = r;
    }

    public void StartVacuumCommand()
    {
        PendingVacuumCommand = true;
        VacuumCommandStartedAtUtc = DateTime.UtcNow;
        VacuumSuccessLogged = false;
        VacuumTimeoutLogged = false;
        VacuumConflictLogged = false;
    }

    public void StopVacuumCommand()
    {
        PendingVacuumCommand = false;
        VacuumCommandStartedAtUtc = null;
        VacuumSuccessLogged = false;
        VacuumTimeoutLogged = false;
        VacuumConflictLogged = false;
    }

    public bool HasVacuumTimedOut(DateTime utcNow)
    {
        return PendingVacuumCommand
            && VacuumCommandStartedAtUtc.HasValue
            && utcNow - VacuumCommandStartedAtUtc.Value >= TimeSpan.FromMilliseconds(VacuumTimeoutMs);
    }
}
