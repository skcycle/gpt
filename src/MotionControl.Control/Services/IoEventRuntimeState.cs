namespace MotionControl.Control.Services;

public sealed class IoEventRuntimeState
{
    public event Action? EventsChanged;

    public IReadOnlyList<IoEventRecord> RecentEvents { get; private set; } = Array.Empty<IoEventRecord>();

    public void Add(IoEventRecord record)
    {
        RecentEvents = RecentEvents
            .Concat(new[] { record })
            .TakeLast(300)
            .ToArray();
        EventsChanged?.Invoke();
    }
}

public sealed class IoEventRecord
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Name { get; init; } = string.Empty;
    public int Address { get; init; }
    public bool IsOutput { get; init; }
    public bool Value { get; init; }
    public string Message { get; init; } = string.Empty;
}
