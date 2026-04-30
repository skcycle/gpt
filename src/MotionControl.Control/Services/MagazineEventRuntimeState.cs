namespace MotionControl.Control.Services;

public sealed class MagazineEventRuntimeState
{
    public event Action? EventsChanged;

    public IReadOnlyList<MagazineEventRecord> RecentEvents { get; private set; } = Array.Empty<MagazineEventRecord>();

    public void Add(MagazineEventRecord record)
    {
        RecentEvents = RecentEvents
            .Concat(new[] { record })
            .TakeLast(200)
            .ToArray();
        EventsChanged?.Invoke();
    }
}

public sealed class MagazineEventRecord
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string MagazineName { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
