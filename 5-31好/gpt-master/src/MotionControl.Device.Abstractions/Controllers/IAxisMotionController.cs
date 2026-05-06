using MotionControl.Device.Abstractions.Models;
using MotionControl.Device.Abstractions.Results;

namespace MotionControl.Device.Abstractions.Controllers;

/// <summary>
/// 轴运动控制能力接口。
/// 只描述轴反馈、使能、回零、运动、停止等运动学相关能力，
/// 不混入 IO、EtherCAT 总线状态或安全急停职责。
/// </summary>
public interface IAxisMotionController
{
    Task<AxisFeedback> GetAxisFeedbackAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> EnableAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> DisableAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> HomeAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> MoveAbsoluteAsync(int axisNo, AxisMoveCommand command, CancellationToken cancellationToken = default);
    Task<DeviceResult> JogAxisAsync(int axisNo, double velocity, bool positiveDirection, CancellationToken cancellationToken = default);
    Task<DeviceResult> StopAxisAsync(int axisNo, CancellationToken cancellationToken = default);
    Task<DeviceResult> ResetAxisAlarmAsync(int axisNo, CancellationToken cancellationToken = default);
}
