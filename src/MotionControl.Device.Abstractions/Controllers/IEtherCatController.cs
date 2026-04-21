using MotionControl.Device.Abstractions.Models;
using MotionControl.Device.Abstractions.Results;

namespace MotionControl.Device.Abstractions.Controllers;

public interface IEtherCatController
{
    Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken = default);
    Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken = default);
    Task<EtherCatControllerStatus> GetControllerStatusAsync(CancellationToken cancellationToken = default);
}
