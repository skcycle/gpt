using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class AlarmPollingService(
    Machine machine,
    ControllerRuntimeState controllerRuntimeState)
{
    public Task PollAsync(CancellationToken cancellationToken = default)
    {
        var controllerStatus = controllerRuntimeState.LastControllerStatus;

        if (controllerStatus is null || !controllerStatus.IsConnected)
        {
            machine.UpsertAlarm("SYS-CONTROLLER-DISCONNECTED", "Controller not connected", "System", "Communication", "Error");
        }
        else
        {
            machine.ClearAlarm("SYS-CONTROLLER-DISCONNECTED");
        }

        foreach (var axis in machine.Axes)
        {
            var code = $"AXIS-{axis.ControllerAxisNo:00}-ALARM";
            var message = $"{axis.Name} axis alarm active";
            if (axis.HasAlarm)
            {
                machine.UpsertAlarm(code, message, axis.Name, "Motion", "Error");
            }
            else
            {
                machine.ClearAlarm(code);
            }
        }

        return Task.CompletedTask;
    }
}
