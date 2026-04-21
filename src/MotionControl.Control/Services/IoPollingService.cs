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
            var previousValue = ioPoint.Value;
            var currentValue = await motionController.GetIoPointValueAsync(ioPoint.Address, ioPoint.IsOutput, cancellationToken);
            if (previousValue != currentValue)
            {
                ioPoint.Update(currentValue);
                ioEventRuntimeState.Add(new IoEventRecord
                {
                    Name = ioPoint.Name,
                    Address = ioPoint.Address,
                    IsOutput = ioPoint.IsOutput,
                    Value = currentValue,
                    Message = $"{ioPoint.Name} -> {(currentValue ? "ON" : "OFF")}"
                });
            }
        }
    }
}
