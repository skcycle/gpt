using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class AlarmItemViewModel
{
    public AlarmItemViewModel(Alarm alarm)
    {
        Code = alarm.Code;
        Message = alarm.Message;
        Source = alarm.Source;
        Category = alarm.Category;
        Severity = alarm.Severity;
        OccurredAt = alarm.OccurredAt;
        IsActive = alarm.IsActive;
    }

    public string Code { get; }
    public string Message { get; }
    public string Source { get; }
    public string Category { get; }
    public string Severity { get; }
    public DateTime OccurredAt { get; }
    public bool IsActive { get; }
}
