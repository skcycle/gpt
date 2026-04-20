using MotionControl.Control.Services;

namespace MotionControl.Presentation.ViewModels;

public sealed class IoEventItemViewModel
{
    public IoEventItemViewModel(CommandFeedback feedback)
    {
        Time = feedback.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
        Direction = feedback.CommandName;
        Address = feedback.AxisNo.HasValue ? feedback.AxisNo.Value.ToString() : "-";
        State = feedback.Status;
        Description = feedback.Message;
    }

    public string Time { get; }
    public string Direction { get; }
    public string Address { get; }
    public string State { get; }
    public string Description { get; }
}
