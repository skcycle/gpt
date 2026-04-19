using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Models;
using MotionControl.Device.Abstractions.Results;
using MotionControl.Device.Zmc.Config;
using MotionControl.Device.Zmc.Native;
using MotionControl.Device.Zmc.Translators;

namespace MotionControl.Device.Zmc.Controllers;

public sealed class ZmcMotionController(
    ZmcControllerOptions options,
    ZmcStatusTranslator statusTranslator,
    ZmcAxisNativeFacade axisNativeFacade) : IMotionController
{
    private bool _isConnected;

    public Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.FromResult(DeviceResult.Ok());
    }

    public Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        return Task.FromResult(DeviceResult.Ok());
    }

    public Task<AxisFeedback> GetAxisFeedbackAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(statusTranslator.Translate(axisNo));

    public Task<DeviceResult> EnableAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.EnableAxis(axisNo) == 0 ? DeviceResult.Ok() : DeviceResult.Fail("ZMC enable axis failed."));

    public Task<DeviceResult> DisableAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.DisableAxis(axisNo) == 0 ? DeviceResult.Ok() : DeviceResult.Fail("ZMC disable axis failed."));

    public Task<DeviceResult> HomeAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<DeviceResult> MoveAbsoluteAsync(int axisNo, AxisMoveCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.MoveAbsolute(axisNo, command.Position, command.Velocity, command.Acceleration, command.Deceleration) == 0
            ? DeviceResult.Ok()
            : DeviceResult.Fail("ZMC move absolute failed."));

    public Task<DeviceResult> StopAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.StopAxis(axisNo) == 0 ? DeviceResult.Ok() : DeviceResult.Fail("ZMC stop axis failed."));

    public Task<DeviceResult> ResetAxisAlarmAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<EtherCatControllerStatus> GetControllerStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new EtherCatControllerStatus
        {
            IsConnected = _isConnected,
            IsOperational = _isConnected,
            OnlineSlaveCount = options.AxisCount,
            NetworkState = _isConnected ? "Operational" : "Disconnected",
            ControllerModel = "ZMC432EtherCAT"
        });
}
