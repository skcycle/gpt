namespace MotionControl.Presentation.ViewModels;

public sealed class ImmediateUiRefreshNotifier : IUiRefreshNotifier
{
    public void RequestRefresh(Action refreshAction)
    {
        refreshAction();
    }
}
