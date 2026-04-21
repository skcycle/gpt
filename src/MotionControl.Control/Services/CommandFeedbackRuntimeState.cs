namespace MotionControl.Control.Services;

public sealed class CommandFeedbackRuntimeState
{
    public event Action? FeedbackChanged;

    public IReadOnlyList<CommandFeedback> RecentFeedback { get; private set; } = Array.Empty<CommandFeedback>();

    public void Add(CommandFeedback feedback)
    {
        RecentFeedback = RecentFeedback
            .Concat(new[] { feedback })
            .TakeLast(100)
            .ToArray();
        FeedbackChanged?.Invoke();
    }

    public void AddStarted(string commandName, int? axisNo = null, string message = "")
        => Add(new CommandFeedback { CommandName = commandName, AxisNo = axisNo, Status = "Started", Message = message });

    public void AddSucceeded(string commandName, int? axisNo = null, string message = "")
        => Add(new CommandFeedback { CommandName = commandName, AxisNo = axisNo, Status = "Succeeded", Message = message });

    public void AddFailed(string commandName, int? axisNo = null, string message = "")
        => Add(new CommandFeedback { CommandName = commandName, AxisNo = axisNo, Status = "Failed", Message = message });
}
