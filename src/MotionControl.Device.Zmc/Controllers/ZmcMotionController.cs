using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Models;
using MotionControl.Device.Abstractions.Results;
using MotionControl.Device.Zmc.Config;
using MotionControl.Device.Zmc.Translators;

namespace MotionControl.Device.Zmc.Controllers;

public sealed class ZmcMotionController(
    ZmcControllerOptions options,
    ZmcStatusTranslator statusTranslator) : IMotionController
{
    public Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<AxisFeedback> GetAxisFeedbackAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(statusTranslator.Translate(axisNo));

    public Task<DeviceResult> EnableAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<DeviceResult> DisableAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<DeviceResult> HomeAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<DeviceResult> MoveAbsoluteAsync(int axisNo, AxisMoveCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<DeviceResult> StopAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<DeviceResult> ResetAxisAlarmAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());
}
