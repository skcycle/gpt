using System;

namespace MotionControl.Presentation.ViewModels;

public sealed class DispatcherUiRefreshNotifier(Action refreshAction) : IUiRefreshNotifier
{
    public void RequestRefresh()
    {
        refreshAction();
    }
}
