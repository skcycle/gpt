using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Interfaces;

public interface IAxisParameterAppService
{
    Task<AxisMappingItem?> LoadAxisParametersAsync(int axisNo, CancellationToken cancellationToken = default);
    Task SaveAxisParametersAsync(AxisMappingItem axisMappingItem, CancellationToken cancellationToken = default);
}
