namespace MotionControl.Application.Interfaces;

public interface IFeedbackEventPublisher
{
    event Action? FeedbackChanged;
}
