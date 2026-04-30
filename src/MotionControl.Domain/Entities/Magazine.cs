namespace MotionControl.Domain.Entities;

using System.Collections.ObjectModel;
using MotionControl.Infrastructure.Configuration;

public sealed class Magazine
{
    public Magazine(
        string name,
        string description,
        int xAxisNo,
        int yAxisNo,
        int zAxisNo,
        int vacuumOutputAddress,
        int blowOutputAddress,
        int materialPresentInputAddress,
        int currentLayerHasMaterialInputAddress,
        int trayKeyingInputAddress,
        int layerCount = 1,
        double layerHeight = 0,
        double pickLiftHeight = 0,
        int actionTimeoutMs = 3000,
        IEnumerable<MagazinePositionConfigItem>? positions = null)
    {
        Name = name;
        Description = description;
        XAxisNo = xAxisNo;
        YAxisNo = yAxisNo;
        ZAxisNo = zAxisNo;
        VacuumOutputAddress = vacuumOutputAddress;
        BlowOutputAddress = blowOutputAddress;
        MaterialPresentInputAddress = materialPresentInputAddress;
        CurrentLayerHasMaterialInputAddress = currentLayerHasMaterialInputAddress;
        TrayKeyingInputAddress = trayKeyingInputAddress;
        LayerCount = layerCount;
        LayerHeight = layerHeight;
        PickLiftHeight = pickLiftHeight;
        ActionTimeoutMs = actionTimeoutMs;
        Positions = new ObservableCollection<MagazinePositionConfigItem>((positions ?? CreateDefaultPositions()).Select(ClonePosition));
        SelectedPositionName = Positions.FirstOrDefault()?.Name;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public int XAxisNo { get; private set; }
    public int YAxisNo { get; private set; }
    public int ZAxisNo { get; private set; }
    public int VacuumOutputAddress { get; private set; }
    public int BlowOutputAddress { get; private set; }
    public int MaterialPresentInputAddress { get; private set; }
    public int CurrentLayerHasMaterialInputAddress { get; private set; }
    public int TrayKeyingInputAddress { get; private set; }
    public int LayerCount { get; private set; }
    public double LayerHeight { get; private set; }
    public double PickLiftHeight { get; private set; }
    public int ActionTimeoutMs { get; private set; }
    public ObservableCollection<MagazinePositionConfigItem> Positions { get; }
    public string? SelectedPositionName { get; set; }

    public bool PendingVacuumCommand { get; private set; }
    public DateTime? VacuumCommandStartedAtUtc { get; private set; }
    public bool VacuumSuccessLogged { get; set; }
    public bool VacuumTimeoutLogged { get; set; }
    public bool VacuumConflictLogged { get; set; }

    public void UpdateMetadata(
        string name,
        string description,
        int xAxisNo,
        int yAxisNo,
        int zAxisNo,
        int vacuumOutputAddress,
        int blowOutputAddress,
        int materialPresentInputAddress,
        int currentLayerHasMaterialInputAddress,
        int trayKeyingInputAddress,
        int layerCount,
        double layerHeight,
        double pickLiftHeight,
        int actionTimeoutMs)
    {
        Name = name;
        Description = description;
        XAxisNo = xAxisNo;
        YAxisNo = yAxisNo;
        ZAxisNo = zAxisNo;
        VacuumOutputAddress = vacuumOutputAddress;
        BlowOutputAddress = blowOutputAddress;
        MaterialPresentInputAddress = materialPresentInputAddress;
        CurrentLayerHasMaterialInputAddress = currentLayerHasMaterialInputAddress;
        TrayKeyingInputAddress = trayKeyingInputAddress;
        LayerCount = layerCount;
        LayerHeight = layerHeight;
        PickLiftHeight = pickLiftHeight;
        ActionTimeoutMs = actionTimeoutMs;
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
        return PendingVacuumCommand && VacuumCommandStartedAtUtc.HasValue && utcNow - VacuumCommandStartedAtUtc.Value >= TimeSpan.FromMilliseconds(ActionTimeoutMs);
    }

    public void AddPosition(MagazinePositionConfigItem position)
    {
        Positions.Add(ClonePosition(position));
        SelectedPositionName = position.Name;
    }

    public bool DeleteSelectedPosition()
    {
        var selected = GetSelectedPosition();
        if (selected is null || IsSystemPosition(selected)) return false;
        Positions.Remove(selected);
        SelectedPositionName = Positions.FirstOrDefault()?.Name;
        return true;
    }

    public MagazinePositionConfigItem? GetSelectedPosition()
    {
        return Positions.FirstOrDefault(p => string.Equals(p.Name, SelectedPositionName, StringComparison.OrdinalIgnoreCase))
               ?? Positions.FirstOrDefault();
    }

    public bool IsSelectedPositionSystemDefault()
    {
        var selected = GetSelectedPosition();
        return selected is not null && IsSystemPosition(selected);
    }

    public void EnsureDefaultPositions()
    {
        EnsureSystemPosition(MagazinePositionKinds.PickStart, "取料起始位");
        EnsureSystemPosition(MagazinePositionKinds.InspectStart, "检测起始位");
        SelectedPositionName ??= Positions.FirstOrDefault()?.Name;
    }

    private void EnsureSystemPosition(string kind, string name)
    {
        var existing = Positions.FirstOrDefault(p => string.Equals(p.Kind, kind, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Name = name;
            existing.Kind = kind;
            return;
        }

        Positions.Insert(Positions.Count > 0 ? Math.Min(1, Positions.Count) : 0, new MagazinePositionConfigItem
        {
            Name = name,
            Description = string.Empty,
            Kind = kind,
            X = 0,
            Y = 0,
            Z = 0
        });

        var ordered = Positions
            .OrderBy(p => GetKindOrder(p.Kind))
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Positions.Clear();
        foreach (var item in ordered) Positions.Add(item);
    }

    private static int GetKindOrder(string? kind)
    {
        return kind switch
        {
            MagazinePositionKinds.PickStart => 0,
            MagazinePositionKinds.InspectStart => 1,
            _ => 2
        };
    }

    private static bool IsSystemPosition(MagazinePositionConfigItem position)
    {
        return string.Equals(position.Kind, MagazinePositionKinds.PickStart, StringComparison.OrdinalIgnoreCase)
               || string.Equals(position.Kind, MagazinePositionKinds.InspectStart, StringComparison.OrdinalIgnoreCase);
    }

    private static MagazinePositionConfigItem ClonePosition(MagazinePositionConfigItem position)
    {
        return new MagazinePositionConfigItem
        {
            Name = position.Name,
            Description = position.Description,
            Kind = string.IsNullOrWhiteSpace(position.Kind) ? MagazinePositionKinds.Normal : position.Kind,
            X = position.X,
            Y = position.Y,
            Z = position.Z
        };
    }

    private static IEnumerable<MagazinePositionConfigItem> CreateDefaultPositions()
    {
        return new[]
        {
            new MagazinePositionConfigItem { Name = "取料起始位", Description = string.Empty, Kind = MagazinePositionKinds.PickStart },
            new MagazinePositionConfigItem { Name = "检测起始位", Description = string.Empty, Kind = MagazinePositionKinds.InspectStart }
        };
    }
}
