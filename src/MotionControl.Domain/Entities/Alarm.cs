namespace MotionControl.Domain.Entities;

public sealed class Alarm
{
    public Alarm(string code, string message, DateTime occurredAt)
    {
        Code = code;
        Message = message;
        OccurredAt = occurredAt;
    }

    public string Code { get; }
    public string Message { get; }
    public DateTime OccurredAt { get; }
    public bool IsActive { get; private set; } = true;

    public void Clear() => IsActive = false;
}
