using System;

namespace MotionControl.Presentation.ViewModels;

public sealed class ImmediateUiRefreshNotifier(Action refreshAction) : IUiRefreshNotifier
{
    public void RequestRefresh()
    {
        refreshAction();
    }
}
