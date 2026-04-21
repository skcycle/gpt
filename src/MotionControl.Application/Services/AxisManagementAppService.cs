using MotionControl.Application.Interfaces;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class AxisManagementAppService(
    IAxisParameterAppService axisParameterAppService,
    IAxisRuntimeParameterSyncService axisRuntimeParameterSyncService) : IAxisManagementAppService
{
    public async Task<AxisMappingItem> AddAxisAsync(CancellationToken cancellationToken = default)
    {
        var item = await axisParameterAppService.AddAxisAsync(cancellationToken);
        await axisRuntimeParameterSyncService.ApplyAsync(item, cancellationToken);
        return item;
    }

    public Task<bool> DeleteAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => axisParameterAppService.DeleteAxisAsync(axisNo, cancellationToken);

    public async Task SaveAxisAsync(AxisMappingItem axisMappingItem, CancellationToken cancellationToken = default)
    {
        await axisParameterAppService.SaveAxisParametersAsync(axisMappingItem, cancellationToken);
        await axisRuntimeParameterSyncService.ApplyAsync(axisMappingItem, cancellationToken);
    }

    public Task<AxisMappingItem?> LoadAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => axisParameterAppService.LoadAxisParametersAsync(axisNo, cancellationToken);
}
