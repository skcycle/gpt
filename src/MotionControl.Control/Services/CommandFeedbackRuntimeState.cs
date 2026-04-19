namespace MotionControl.Control.Services;

public sealed class CommandFeedbackRuntimeState
{
    public IReadOnlyList<CommandFeedback> RecentFeedback { get; private set; } = Array.Empty<CommandFeedback>();

    public void Add(CommandFeedback feedback)
    {
        RecentFeedback = RecentFeedback
            .Concat(new[] { feedback })
            .TakeLast(20)
            .ToArray();
    }
}
