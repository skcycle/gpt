namespace MotionControl.Domain.Entities;

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
        int actionTimeoutMs = 3000)
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
}
