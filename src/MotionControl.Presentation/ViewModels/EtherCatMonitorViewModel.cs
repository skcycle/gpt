namespace MotionControl.Presentation.ViewModels;

public sealed class EtherCatMonitorViewModel
{
    public EtherCatMonitorViewModel(DashboardViewModel dashboardViewModel)
    {
        Dashboard = dashboardViewModel;
    }

    public DashboardViewModel Dashboard { get; }
}
