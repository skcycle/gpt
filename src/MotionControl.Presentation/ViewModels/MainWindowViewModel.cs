using System;
using MotionControl.Application.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class MainWindowViewModel
{
    private readonly ISystemAppService _systemAppService;
    private readonly ControllerRuntimeState _controllerRuntimeState;
    private DateTime _lastDashboardRefreshUtc = DateTime.MinValue;
    private DateTime _lastAxisRefreshUtc = DateTime.MinValue;
    private DateTime _lastAlarmRefreshUtc = DateTime.MinValue;

    public MainWindowViewModel(
        Machine machine,
        ISystemAppService systemAppService,
        IMotionAppService motionAppService,
        ControllerRuntimeState controllerRuntimeState,
        HomePlanRuntimeState homePlanRuntimeState,
        CommandFeedbackRuntimeState commandFeedbackRuntimeState)
    {
        _systemAppService = systemAppService;
        _controllerRuntimeState = controllerRuntimeState;
        Dashboard = new DashboardViewModel(machine, commandFeedbackRuntimeState);
        EtherCatMonitor = new EtherCatMonitorViewModel(Dashboard);
        AxisMonitor = new AxisMonitorViewModel(machine);
        AxisDebug = new AxisDebugViewModel(motionAppService, machine, homePlanRuntimeState);
        Alarm = new AlarmViewModel(machine);
    }

    public DashboardViewModel Dashboard { get; }
    public EtherCatMonitorViewModel EtherCatMonitor { get; }
    public AxisMonitorViewModel AxisMonitor { get; }
    public AxisDebugViewModel AxisDebug { get; }
    public AlarmViewModel Alarm { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _systemAppService.InitializeAsync(cancellationToken);
        RefreshViewModels(force: true);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _systemAppService.RefreshAsync(cancellationToken);
        RefreshViewModels(force: true);
    }

    public void RefreshViewModels()
    {
        RefreshViewModels(force: false);
    }

    public void RefreshViewModels(bool force)
    {
        var now = DateTime.UtcNow;

        if (force || now - _lastDashboardRefreshUtc >= TimeSpan.FromMilliseconds(500))
        {
            Dashboard.Refresh(_controllerRuntimeState.LastControllerStatus);
            _lastDashboardRefreshUtc = now;
        }

        if (force || now - _lastAxisRefreshUtc >= TimeSpan.FromMilliseconds(500))
        {
            AxisMonitor.RefreshAll();
            _lastAxisRefreshUtc = now;
        }

        if (force || now - _lastAlarmRefreshUtc >= TimeSpan.FromMilliseconds(1000))
        {
            Alarm.Refresh();
            _lastAlarmRefreshUtc = now;
        }
    }
}
