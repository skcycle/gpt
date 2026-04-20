using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class AlarmPollingService(
    Machine machine,
    ControllerRuntimeState controllerRuntimeState,
    CommandFeedbackRuntimeState commandFeedbackRuntimeState)
{
    public Task PollAsync(CancellationToken cancellationToken = default)
    {
        var controllerStatus = controllerRuntimeState.LastControllerStatus;

        if (controllerStatus is null || !controllerStatus.IsConnected)
        {
            if (machine.UpsertAlarm("SYS-CONTROLLER-DISCONNECTED", "Controller not connected", "System", "Communication", "Error"))
            {
                commandFeedbackRuntimeState.Add(new CommandFeedback
                {
                    CommandName = "Alarm",
                    Status = "Raised",
                    Message = "Controller disconnect alarm raised"
                });
            }
        }
        else if (machine.ClearAlarm("SYS-CONTROLLER-DISCONNECTED"))
        {
            commandFeedbackRuntimeState.Add(new CommandFeedback
            {
                CommandName = "Alarm",
                Status = "Cleared",
                Message = "Controller disconnect alarm cleared"
            });
        }

        foreach (var axis in machine.Axes)
        {
            var code = $"AXIS-{axis.ControllerAxisNo:00}-ALARM";
            var message = $"{axis.Name} axis alarm active";
            if (axis.HasAlarm)
            {
                if (machine.UpsertAlarm(code, message, axis.Name, "Motion", "Error"))
                {
                    commandFeedbackRuntimeState.Add(new CommandFeedback
                    {
                        CommandName = "Alarm",
                        AxisNo = axis.ControllerAxisNo,
                        Status = "Raised",
                        Message = message
                    });
                }
            }
            else if (machine.ClearAlarm(code))
            {
                commandFeedbackRuntimeState.Add(new CommandFeedback
                {
                    CommandName = "Alarm",
                    AxisNo = axis.ControllerAxisNo,
                    Status = "Cleared",
                    Message = $"{axis.Name} axis alarm cleared"
                });
            }
        }

        return Task.CompletedTask;
    }
}
