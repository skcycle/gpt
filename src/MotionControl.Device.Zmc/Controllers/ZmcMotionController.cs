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
    ZmcAxisNativeFacade axisNativeFacade,
    IEtherCatStatusProvider etherCatStatusProvider) : IAxisMotionController, IIoController, IEtherCatController, ISafetyController
{
    public async Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            var result = axisNativeFacade.Connect(options.IpAddress);
            return result == 0 ? DeviceResult.Ok() : DeviceResult.Fail("控制器连接失败");
        }, cancellationToken);
    }

    public async Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            var result = axisNativeFacade.Disconnect();
            return result == 0 ? DeviceResult.Ok() : DeviceResult.Fail("控制器断开连接失败");
        }, cancellationToken);
    }

    public async Task<AxisFeedback> GetAxisFeedbackAsync(int axisNo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            float dpos = 0;
            float mpos = 0;
            float speed = 0;
            var idle = 1;
            var axisStatus = 0;
            var homeStatus = 0;
            var busEnableStatus = 0;

            axisNativeFacade.GetAxisDpos(axisNo, ref dpos);
            axisNativeFacade.GetAxisMpos(axisNo, ref mpos);
            axisNativeFacade.GetAxisSpeed(axisNo, ref speed);

            var status2Result = axisNativeFacade.GetAxisStatus2(axisNo, -1, ref axisStatus, ref idle, ref homeStatus, ref busEnableStatus);
            if (status2Result != 0)
            {
                axisNativeFacade.GetAxisIdle(axisNo, ref idle);
                axisNativeFacade.GetAxisStatus(axisNo, ref axisStatus);
            }

            var axisEnable = 0;
            axisNativeFacade.GetAxisEnable(axisNo, ref axisEnable);

            return statusTranslator.Translate(axisNo, dpos, mpos, speed, idle, axisStatus, homeStatus, axisEnable);
        }, cancellationToken);
    }

    public async Task<DeviceResult> EnableAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => await RunCommandAsync(() => axisNativeFacade.EnableAxis(axisNo),
            success: DeviceResult.Ok(),
            failure: DeviceResult.Fail($"轴 {axisNo} 使能失败"),
            cancellationToken);

    public async Task<DeviceResult> DisableAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => await RunCommandAsync(() => axisNativeFacade.DisableAxis(axisNo),
            success: DeviceResult.Ok(),
            failure: DeviceResult.Fail($"轴 {axisNo} 失能失败"),
            cancellationToken);

    public async Task<DeviceResult> HomeAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => await RunCommandAsync(() => axisNativeFacade.HomeAxis(axisNo),
            success: DeviceResult.Ok(),
            failure: DeviceResult.Fail($"轴 {axisNo} 回零失败"),
            cancellationToken);

    public async Task<DeviceResult> MoveAbsoluteAsync(int axisNo, AxisMoveCommand command, CancellationToken cancellationToken = default)
        => await RunCommandAsync(
            () => axisNativeFacade.MoveAbsolute(axisNo, command.Position, command.Velocity, command.Acceleration, command.Deceleration),
            success: DeviceResult.Ok(),
            failure: DeviceResult.Fail($"轴 {axisNo} 定位失败"),
            cancellationToken);

    public async Task<DeviceResult> JogAxisAsync(int axisNo, double velocity, bool positiveDirection, CancellationToken cancellationToken = default)
        => await RunCommandAsync(() => axisNativeFacade.JogAxis(axisNo, velocity, positiveDirection),
            success: DeviceResult.Ok(),
            failure: DeviceResult.Fail($"轴 {axisNo} Jog 失败"),
            cancellationToken);

    public async Task<DeviceResult> StopAxisAsync(int axisNo, CancellationToken cancellationToken = default)
        => await RunCommandAsync(() => axisNativeFacade.StopAxis(axisNo),
            success: DeviceResult.Ok(),
            failure: DeviceResult.Fail($"轴 {axisNo} 停止失败"),
            cancellationToken);

    public async Task<DeviceResult> RapidStopAsync(int mode, CancellationToken cancellationToken = default)
        => await RunCommandAsync(() => axisNativeFacade.RapidStop(mode),
            success: DeviceResult.Ok(),
            failure: DeviceResult.Fail("快速停止失败"),
            cancellationToken);

    public async Task<DeviceResult> ResetAxisAlarmAsync(int axisNo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            var driveResult = axisNativeFacade.ClearDriveAlarm(axisNo);
            if (driveResult != 0)
                return DeviceResult.Fail($"轴 {axisNo} 驱动器报警清除失败");

            var zmcResult = axisNativeFacade.ClearZmcAlarm(axisNo);
            if (zmcResult != 0)
                return DeviceResult.Fail($"轴 {axisNo} 自身报警清除失败");

            return DeviceResult.Ok();
        }, cancellationToken);
    }

    public async Task<bool> GetIoPointValueAsync(int address, bool isOutput, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            uint value = 0;
            if (isOutput)
            {
                axisNativeFacade.GetOutput(address, ref value);
            }
            else
            {
                axisNativeFacade.GetInput(address, ref value);
            }
            return value != 0;
        }, cancellationToken);
    }

    public async Task<DeviceResult> SetIoPointValueAsync(int address, bool value, CancellationToken cancellationToken = default)
        => await RunCommandAsync(() => axisNativeFacade.SetOutput(address, value ? 1 : 0),
            success: DeviceResult.Ok(),
            failure: DeviceResult.Fail($"DO {address} 输出失败"),
            cancellationToken);

    public async Task<EtherCatControllerStatus> GetControllerStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            if (axisNativeFacade.IsConnected)
            {
                var probeResult = axisNativeFacade.ProbeConnection();
                if (probeResult != 0)
                {
                    axisNativeFacade.Disconnect();
                }
            }

            return etherCatStatusProvider.CreateStatus(axisNativeFacade.IsConnected, options.AxisCount);
        }, cancellationToken);
    }

    private static async Task<DeviceResult> RunCommandAsync(
        Func<int> command,
        DeviceResult success,
        DeviceResult failure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() => command() == 0 ? success : failure, cancellationToken);
    }
}
