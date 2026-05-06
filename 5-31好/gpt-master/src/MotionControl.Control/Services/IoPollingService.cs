using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class IoPollingService(
    IIoController motionController,
    Machine machine,
    IoEventRuntimeState ioEventRuntimeState)
{
    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        foreach (var ioPoint in machine.IoPoints)
        {
            bool currentValue;
            try
            {
                // GetIoPointValueAsync 失败时会抛异常（不再静默返回 false）
                // 单个 IO 点读取失败不影响其他点的轮询
                currentValue = await motionController.GetIoPointValueAsync(ioPoint.Address, ioPoint.IsOutput, cancellationToken);
            }
            catch
            {
                // 本 IO 点读取失败，跳过本轮更新
                continue;
            }

            var previousValue = ioPoint.Value;
            if (previousValue != currentValue)
            {
                ioPoint.Update(currentValue);
                ioEventRuntimeState.Add(new IoEventRecord
                {
                    Name = ioPoint.Name,
                    Address = ioPoint.Address,
                    IsOutput = ioPoint.IsOutput,
                    Value = currentValue,
                    Message = $"{ioPoint.Name} -> {(currentValue ? "ON" : "OFF")}",
                });
            }
        }
    }
}
