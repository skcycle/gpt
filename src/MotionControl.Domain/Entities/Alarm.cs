namespace MotionControl.Domain.Entities;

public sealed class Alarm
{
    public Alarm(string code, string message, DateTime occurredAt, string source = "System", string category = "General", string severity = "Error")
    {
        Code = code;
        Message = message;
        OccurredAt = occurredAt;
        Source = source;
        Category = category;
        Severity = severity;
    }

    public string Code { get; }
    public string Message { get; }
    public DateTime OccurredAt { get; }
    public string Source { get; }
    public string Category { get; }
    public string Severity { get; }
    public bool IsActive { get; private set; } = true;

    public void Clear() => IsActive = false;
}
