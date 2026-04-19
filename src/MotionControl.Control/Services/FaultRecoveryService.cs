using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Control.Services;

public sealed class FaultRecoveryService(CommandFeedbackRuntimeState commandFeedbackRuntimeState)
{
    public void BeginRecovery(Machine machine)
    {
        machine.SetSystemState(SystemState.FaultRecovering);
        commandFeedbackRuntimeState.Add(new CommandFeedback
        {
            CommandName = "FaultRecovery",
            Status = "Running",
            Message = "System entered fault recovery state"
        });
    }

    public void CompleteRecovery(Machine machine)
    {
        machine.SetSystemState(SystemState.Standby);
        commandFeedbackRuntimeState.Add(new CommandFeedback
        {
            CommandName = "FaultRecovery",
            Status = "Succeeded",
            Message = "System fault recovery completed"
        });
    }
}
