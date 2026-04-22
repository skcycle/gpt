namespace MotionControl.Control.Services;

public sealed class WorkHeadEventRuntimeState
{
    public event Action? EventsChanged;

    public IReadOnlyList<WorkHeadEventRecord> RecentEvents { get; private set; } = Array.Empty<WorkHeadEventRecord>();

    public void Add(WorkHeadEventRecord record)
    {
        RecentEvents = RecentEvents
            .Concat(new[] { record })
            .TakeLast(200)
            .ToArray();
        EventsChanged?.Invoke();
    }
}

public sealed class WorkHeadEventRecord
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string WorkHeadName { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
