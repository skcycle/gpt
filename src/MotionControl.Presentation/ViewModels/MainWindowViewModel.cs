using MotionControl.Application.Interfaces;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class MainWindowViewModel
{
    private readonly ISystemAppService _systemAppService;

    public MainWindowViewModel(
        Machine machine,
        ISystemAppService systemAppService,
        IMotionAppService motionAppService)
    {
        _systemAppService = systemAppService;
        Dashboard = new DashboardViewModel(machine);
        AxisMonitor = new AxisMonitorViewModel(machine);
        AxisDebug = new AxisDebugViewModel(motionAppService);
        Alarm = new AlarmViewModel(machine);
    }

    public DashboardViewModel Dashboard { get; }
    public AxisMonitorViewModel AxisMonitor { get; }
    public AxisDebugViewModel AxisDebug { get; }
    public AlarmViewModel Alarm { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _systemAppService.InitializeAsync(cancellationToken);
        RefreshViewModels();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _systemAppService.RefreshAsync(cancellationToken);
        RefreshViewModels();
    }

    public void RefreshViewModels()
    {
        Dashboard.Refresh();
        AxisMonitor.RefreshAll();
        Alarm.Refresh();
    }
}
