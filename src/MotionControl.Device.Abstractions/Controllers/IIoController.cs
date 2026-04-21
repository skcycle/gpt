using MotionControl.Device.Abstractions.Results;

namespace MotionControl.Device.Abstractions.Controllers;

/// <summary>
/// IO 读写能力接口。
/// 只负责 DI/DO 的读取与写入，避免和轴运动控制能力耦合。
/// </summary>
public interface IIoController
{
    Task<bool> GetIoPointValueAsync(int address, bool isOutput, CancellationToken cancellationToken = default);
    Task<DeviceResult> SetIoPointValueAsync(int address, bool value, CancellationToken cancellationToken = default);
}
