namespace MotionControl.Presentation.ViewModels;

public interface IUiRefreshNotifier
{
    void RequestRefresh(Action refreshAction);
}
