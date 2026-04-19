namespace MotionControl.Presentation.ViewModels;

public sealed class DispatcherUiRefreshNotifier : IUiRefreshNotifier
{
    public void RequestRefresh(Action refreshAction)
    {
        refreshAction();
    }
}
