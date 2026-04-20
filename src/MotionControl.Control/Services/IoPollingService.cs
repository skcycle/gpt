using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class IoPollingService(
    IMotionController motionController,
    Machine machine,
    CommandFeedbackRuntimeState commandFeedbackRuntimeState)
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
                commandFeedbackRuntimeState.Add(new CommandFeedback
                {
                    CommandName = ioPoint.IsOutput ? "DO" : "DI",
                    Status = "Changed",
                    Message = $"{ioPoint.Name} -> {(currentValue ? "ON" : "OFF")}"
                });
            }
        }
    }
}
