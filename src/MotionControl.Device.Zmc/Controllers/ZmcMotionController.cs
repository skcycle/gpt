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
        var result = axisNativeFacade.Connect(options.IpAddress);
        _isConnected = result == 0;
        return Task.FromResult(_isConnected ? DeviceResult.Ok() : DeviceResult.Fail($"ZMC connect failed: {result}"));
    }

    public Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        var result = axisNativeFacade.Disconnect();
        _isConnected = false;
        return Task.FromResult(result == 0 ? DeviceResult.Ok() : DeviceResult.Fail($"ZMC disconnect failed: {result}"));
    }

    public Task<AxisFeedback> GetAxisFeedbackAsync(int axisNo, CancellationToken cancellationToken = default)
    {
        float dpos = 0;
        float speed = 0;
        var idle = 1;
        var axisStatus = 0;
        var homeStatus = 0;
        var busEnableStatus = 0;

        axisNativeFacade.GetAxisDpos(axisNo, ref dpos);
        axisNativeFacade.GetAxisSpeed(axisNo, ref speed);

        var status2Result = axisNativeFacade.GetAxisStatus2(axisNo, -1, ref axisStatus, ref idle, ref homeStatus, ref busEnableStatus);
        if (status2Result != 0)
        {
            axisNativeFacade.GetAxisIdle(axisNo, ref idle);
            axisNativeFacade.GetAxisStatus(axisNo, ref axisStatus);
        }

        return Task.FromResult(statusTranslator.Translate(axisNo, dpos, speed, idle, axisStatus, homeStatus, busEnableStatus));
    }

    public Task<DeviceResult> EnableAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.EnableAxis(axisNo) == 0 ? DeviceResult.Ok() : DeviceResult.Fail("ZMC enable axis failed."));

    public Task<DeviceResult> DisableAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.DisableAxis(axisNo) == 0 ? DeviceResult.Ok() : DeviceResult.Fail("ZMC disable axis failed."));

    public Task<DeviceResult> HomeAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.HomeAxis(axisNo) == 0 ? DeviceResult.Ok() : DeviceResult.Fail("ZMC home axis failed."));

    public Task<DeviceResult> MoveAbsoluteAsync(int axisNo, AxisMoveCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.MoveAbsolute(axisNo, command.Position, command.Velocity, command.Acceleration, command.Deceleration) == 0
            ? DeviceResult.Ok()
            : DeviceResult.Fail("ZMC move absolute failed."));

    public Task<DeviceResult> JogAxisAsync(int axisNo, double velocity, bool positiveDirection, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.JogAxis(axisNo, velocity, positiveDirection) == 0 ? DeviceResult.Ok() : DeviceResult.Fail("ZMC jog axis failed."));

    public Task<DeviceResult> StopAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(axisNativeFacade.StopAxis(axisNo) == 0 ? DeviceResult.Ok() : DeviceResult.Fail("ZMC stop axis failed."));

    public Task<DeviceResult> ResetAxisAlarmAsync(int axisNo, CancellationToken cancellationToken = default)
        => Task.FromResult(DeviceResult.Ok());

    public Task<EtherCatControllerStatus> GetControllerStatusAsync(CancellationToken cancellationToken = default)
    {
        var slaves = Enumerable.Range(1, Math.Min(options.AxisCount, 4))
            .Select(index => new EtherCatSlaveStatus
            {
                SlaveNo = index,
                Name = $"Servo-{index:00}",
                State = _isConnected ? "OP" : "INIT",
                ModuleType = index == 1 ? "Coupler" : "Servo",
                FaultText = _isConnected ? string.Empty : "Controller offline",
                IsOnline = _isConnected,
                HasAlarm = false
            })
            .ToArray();

        var onlineSlaveCount = slaves.Count(slave => slave.IsOnline);
        var offlineSlaveCount = slaves.Length - onlineSlaveCount;
        var alarmSlaveCount = slaves.Count(slave => slave.HasAlarm);

        return Task.FromResult(new EtherCatControllerStatus
        {
            IsConnected = _isConnected,
            IsOperational = _isConnected,
            ExpectedSlaveCount = slaves.Length,
            OnlineSlaveCount = onlineSlaveCount,
            OfflineSlaveCount = offlineSlaveCount,
            AlarmSlaveCount = alarmSlaveCount,
            HasOfflineSlave = offlineSlaveCount > 0,
            HasAnySlaveAlarm = alarmSlaveCount > 0,
            SummaryState = !_isConnected ? "Disconnected" : alarmSlaveCount > 0 ? "Alarm" : offlineSlaveCount > 0 ? "Warning" : "Healthy",
            NetworkState = _isConnected ? "Operational" : "Disconnected",
            ControllerModel = "ZMC432EtherCAT",
            Slaves = slaves
        });
    }
}
