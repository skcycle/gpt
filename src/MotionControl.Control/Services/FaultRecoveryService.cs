using MotionControl.Control.StateMachines;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class FaultRecoveryService(
    CommandFeedbackRuntimeState commandFeedbackRuntimeState,
    SystemStateMachine systemStateMachine)
{
    public void BeginRecovery(Machine machine)
    {
        machine.SetSystemState(systemStateMachine.OnRecoveryStarted());
        commandFeedbackRuntimeState.Add(new CommandFeedback
        {
            CommandName = "FaultRecovery",
            Status = "Running",
            Message = "System entered fault recovery state"
        });
    }

    public void CompleteRecovery(Machine machine)
    {
        machine.SetSystemState(systemStateMachine.OnRecoveryCompleted());
        commandFeedbackRuntimeState.Add(new CommandFeedback
        {
            CommandName = "FaultRecovery",
            Status = "Succeeded",
            Message = "System fault recovery completed"
        });
    }
}
