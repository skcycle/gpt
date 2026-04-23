using System.Collections.ObjectModel;

namespace MotionControl.Domain.Entities;

public sealed class WorkHead
{
    public WorkHead(string name, string description, int xAxisNo, int yAxisNo, int zAxisNo, int rAxisNo, int vacuumOutputAddress, int blowOutputAddress, int vacuumInputAddress, int generalOutputAddress1, int generalOutputAddress2, int generalInputAddress1, int generalInputAddress2, int vacuumTimeoutMs = 3000, IEnumerable<WorkHeadPosition>? positions = null, double safeZ = 0)
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
        Positions = positions is null ? new ObservableCollection<WorkHeadPosition>() : new ObservableCollection<WorkHeadPosition>(positions);
        SafeZ = safeZ;
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
    public double SafeZ { get; private set; }

    public bool PendingVacuumCommand { get; private set; }
    public DateTime? VacuumCommandStartedAtUtc { get; private set; }
    public bool VacuumSuccessLogged { get; set; }
    public bool VacuumTimeoutLogged { get; set; }
    public bool VacuumConflictLogged { get; set; }

    public ObservableCollection<WorkHeadPosition> Positions { get; } = new();
    public string? SelectedPositionName { get; set; }

    public void UpdateMetadata(string name, string description, int xAxisNo, int yAxisNo, int zAxisNo, int rAxisNo, int vacuumOutputAddress, int blowOutputAddress, int vacuumInputAddress, int generalOutputAddress1, int generalOutputAddress2, int generalInputAddress1, int generalInputAddress2, int vacuumTimeoutMs, double safeZ)
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
        SafeZ = safeZ;
    }

    public void AddPosition(WorkHeadPosition position) => Positions.Add(position);
    public void RemovePosition(string positionName)
    {
        var targets = Positions.Where(p => p.Name == positionName).ToList();
        foreach (var target in targets)
        {
            Positions.Remove(target);
        }
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
        return PendingVacuumCommand && VacuumCommandStartedAtUtc.HasValue && utcNow - VacuumCommandStartedAtUtc.Value >= TimeSpan.FromMilliseconds(VacuumTimeoutMs);
    }
}
