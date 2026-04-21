using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Abstractions.Results;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class IoControlService(IIoController motionController, Machine machine, IoEventRuntimeState ioEventRuntimeState)
{
    public async Task<DeviceResult> SetOutputAsync(int address, bool value, CancellationToken cancellationToken = default)
    {
        var result = await motionController.SetIoPointValueAsync(address, value, cancellationToken);
        if (!result.Success)
        {
            return result;
        }

        var ioPoint = machine.IoPoints.FirstOrDefault(x => x.IsOutput && x.Address == address);
        if (ioPoint is not null)
        {
            ioEventRuntimeState.Add(new IoEventRecord
            {
                Name = ioPoint.Name,
                Address = ioPoint.Address,
                IsOutput = true,
                Value = value,
                Message = $"{ioPoint.Name} -> {(value ? "ON" : "OFF")}"
            });
        }

        return result;
    }
}
