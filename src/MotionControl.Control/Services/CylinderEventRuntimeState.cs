namespace MotionControl.Control.Services;

public sealed class CylinderEventRuntimeState
{
    public event Action? EventsChanged;

    public IReadOnlyList<CylinderEventRecord> RecentEvents { get; private set; } = Array.Empty<CylinderEventRecord>();

    public void Add(CylinderEventRecord record)
    {
        RecentEvents = RecentEvents
            .Concat(new[] { record })
            .TakeLast(200)
            .ToArray();
        EventsChanged?.Invoke();
    }
}

public sealed class CylinderEventRecord
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string CylinderName { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
