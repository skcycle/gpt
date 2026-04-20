namespace MotionControl.Application.Interfaces;

public interface ISystemAppService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
    Task EmergencyStopAsync(CancellationToken cancellationToken = default);
    Task ClearEmergencyStopAsync(CancellationToken cancellationToken = default);
}
