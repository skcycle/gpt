using MotionControl.Device.Abstractions.Models;
using MotionControl.Device.Abstractions.Results;

namespace MotionControl.Device.Abstractions.Controllers;

public interface IMotionController
{
    Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken = default);
    Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken = default);
    Task<AxisFeedback> GetAxisFeedbackAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> EnableAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> DisableAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> HomeAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> MoveAbsoluteAsync(int axisNo, AxisMoveCommand command, CancellationToken cancellationToken = default);
    Task<DeviceResult> JogAxisAsync(int axisNo, double velocity, bool positiveDirection, CancellationToken cancellationToken = default);
    Task<DeviceResult> StopAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> RapidStopAsync(int mode, CancellationToken cancellationToken = default);
    Task<DeviceResult> ResetAxisAlarmAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<bool> GetIoPointValueAsync(int address, bool isOutput, CancellationToken cancellationToken = default);
    Task<DeviceResult> SetIoPointValueAsync(int address, bool value, CancellationToken cancellationToken = default);
    Task<EtherCatControllerStatus> GetControllerStatusAsync(CancellationToken cancellationToken = default);
}
