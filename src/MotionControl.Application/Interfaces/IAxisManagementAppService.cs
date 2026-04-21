using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Interfaces;

public interface IAxisManagementAppService
{
    Task<AxisMappingItem> AddAxisAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task SaveAxisAsync(AxisMappingItem axisMappingItem, CancellationToken cancellationToken = default);
    Task<AxisMappingItem?> LoadAxisAsync(int axisNo, CancellationToken cancellationToken = default);
}
