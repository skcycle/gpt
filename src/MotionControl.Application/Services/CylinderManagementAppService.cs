using MotionControl.Application.Interfaces;
using MotionControl.Application.Validation;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class CylinderManagementAppService(
    ICylinderConfigAppService cylinderConfigAppService,
    ICylinderRuntimeSyncService cylinderRuntimeSyncService) : ICylinderManagementAppService
{
    public async Task<CylinderConfigItem> AddCylinderAsync(CancellationToken cancellationToken = default)
    {
        var item = await cylinderConfigAppService.AddCylinderAsync(cancellationToken);
        await cylinderRuntimeSyncService.ApplyAsync(item, cancellationToken);
        return item;
    }

    public async Task<bool> DeleteCylinderAsync(string name, CancellationToken cancellationToken = default)
    {
        var removed = await cylinderConfigAppService.DeleteCylinderAsync(name, cancellationToken);
        if (!removed)
        {
            return false;
        }

        await cylinderRuntimeSyncService.RemoveAsync(name, cancellationToken);
        return true;
    }

    public async Task SaveCylindersAsync(IEnumerable<CylinderConfigItem> cylinders, CancellationToken cancellationToken = default)
    {
        var items = cylinders.ToList();
        CylinderConfigValidator.Validate(items);
        await cylinderConfigAppService.SaveCylindersAsync(items, cancellationToken);
        await cylinderRuntimeSyncService.ReloadAsync(items, cancellationToken);
    }

    public async Task<IReadOnlyList<CylinderConfigItem>> LoadCylindersAsync(CancellationToken cancellationToken = default)
    {
        var items = await cylinderConfigAppService.LoadCylindersAsync(cancellationToken);
        await cylinderRuntimeSyncService.ReloadAsync(items, cancellationToken);
        return items;
    }
}
