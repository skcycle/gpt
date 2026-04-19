using System.Windows.Threading;

namespace MotionControl.Presentation.ViewModels;

public sealed class DispatcherUiRefreshNotifier(Dispatcher dispatcher) : IUiRefreshNotifier
{
    public void RequestRefresh(Action refreshAction)
    {
        if (dispatcher.CheckAccess())
        {
            refreshAction();
            return;
        }

        dispatcher.Invoke(refreshAction);
    }
}
