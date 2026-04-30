using MotionControl.Application.Interfaces;
using MotionControl.Domain.Entities;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class MagazineRuntimeSyncService(Machine machine) : IMagazineRuntimeSyncService
{
    public Task ApplyAsync(MagazineConfigItem magazine, CancellationToken cancellationToken = default)
    {
        EnsureIoPointExists(magazine.VacuumOutputAddress, true);
        EnsureIoPointExists(magazine.BlowOutputAddress, true);
        EnsureIoPointExists(magazine.MaterialPresentInputAddress, false);
        EnsureIoPointExists(magazine.CurrentLayerHasMaterialInputAddress, false);
        EnsureIoPointExists(magazine.TrayKeyingInputAddress, false);

        var existing = machine.Magazines.FirstOrDefault(item => string.Equals(item.Name, magazine.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            machine.AddMagazine(new Magazine(magazine.Name, magazine.Description, magazine.XAxisNo, magazine.YAxisNo, magazine.ZAxisNo, magazine.VacuumOutputAddress, magazine.BlowOutputAddress, magazine.MaterialPresentInputAddress, magazine.CurrentLayerHasMaterialInputAddress, magazine.TrayKeyingInputAddress, magazine.LayerCount, magazine.LayerHeight, magazine.PickLiftHeight, magazine.ActionTimeoutMs));
            return Task.CompletedTask;
        }

        existing.UpdateMetadata(magazine.Name, magazine.Description, magazine.XAxisNo, magazine.YAxisNo, magazine.ZAxisNo, magazine.VacuumOutputAddress, magazine.BlowOutputAddress, magazine.MaterialPresentInputAddress, magazine.CurrentLayerHasMaterialInputAddress, magazine.TrayKeyingInputAddress, magazine.LayerCount, magazine.LayerHeight, magazine.PickLiftHeight, magazine.ActionTimeoutMs);
        return Task.CompletedTask;
    }

    public Task ReloadAsync(IEnumerable<MagazineConfigItem> magazines, CancellationToken cancellationToken = default)
    {
        foreach (var item in machine.Magazines.ToList()) machine.RemoveMagazine(item.Name);
        foreach (var item in magazines) ApplyAsync(item, cancellationToken);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        machine.RemoveMagazine(name);
        return Task.CompletedTask;
    }

    private void EnsureIoPointExists(int address, bool isOutput)
    {
        if (address < 0 || machine.IoPoints.Any(item => item.IsOutput == isOutput && item.Address == address)) return;
        machine.AddIoPoint(new IoPoint($"{(isOutput ? "DO" : "DI")} {address}", address, isOutput, "Auto-created for Magazine"));
    }
}
