using MotionControl.Control.StateMachines;
using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

public sealed class FaultRecoveryService(
    CommandFeedbackRuntimeState commandFeedbackRuntimeState,
    ControllerRuntimeState controllerRuntimeState,
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
        var nextState = systemStateMachine.OnRecoveryCompleted(machine, controllerRuntimeState.LastControllerStatus);
        machine.SetSystemState(nextState);
        commandFeedbackRuntimeState.Add(new CommandFeedback
        {
            CommandName = "FaultRecovery",
            Status = nextState == MotionControl.Domain.Enums.SystemState.FaultRecovering ? "Blocked" : "Succeeded",
            Message = nextState == MotionControl.Domain.Enums.SystemState.FaultRecovering
                ? "Fault recovery cannot complete while controller/alarm conditions remain active"
                : "System fault recovery completed"
        });
    }
}
