using System;
using System.Windows.Input;
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
    private DateTime _lastIoRefreshUtc = DateTime.MinValue;
    private DateTime _lastIoEventRefreshUtc = DateTime.MinValue;

    public MainWindowViewModel(
        Machine machine,
        ISystemAppService systemAppService,
        IMotionAppService motionAppService,
        IAxisParameterAppService axisParameterAppService,
        IAxisRuntimeParameterSyncService axisRuntimeParameterSyncService,
        IAxisControllerParameterAppService axisControllerParameterAppService,
        ControllerRuntimeState controllerRuntimeState,
        HomePlanRuntimeState homePlanRuntimeState,
        CommandFeedbackRuntimeState commandFeedbackRuntimeState)
    {
        _systemAppService = systemAppService;
        _controllerRuntimeState = controllerRuntimeState;
        Dashboard = new DashboardViewModel(machine, commandFeedbackRuntimeState);
        EtherCatMonitor = new EtherCatMonitorViewModel(Dashboard);
        AxisMonitor = new AxisMonitorViewModel(machine);
        IoMonitor = new IoMonitorViewModel(machine);
        IoEventLog = new IoEventLogViewModel(commandFeedbackRuntimeState);
        AxisDebug = new AxisDebugViewModel(motionAppService, machine, homePlanRuntimeState);
        AxisParameterEditor = new AxisParameterEditorViewModel(axisParameterAppService, axisRuntimeParameterSyncService, axisControllerParameterAppService);
        AxisMonitor.SelectedAxisChanged += async axis => await HandleSelectedAxisChangedAsync(axis);
        AxisDebug.SelectedAxisChanged += async axisNo => await AxisParameterEditor.SyncAxisNoAsync(axisNo);
        Alarm = new AlarmViewModel(machine);
        EmergencyStopCommand = new RelayCommand(async () => await _systemAppService.EmergencyStopAsync());
        ClearEmergencyStopCommand = new RelayCommand(async () => await _systemAppService.ClearEmergencyStopAsync());
    }

    public DashboardViewModel Dashboard { get; }
    public EtherCatMonitorViewModel EtherCatMonitor { get; }
    public AxisMonitorViewModel AxisMonitor { get; }
    public AxisDebugViewModel AxisDebug { get; }
    public IoMonitorViewModel IoMonitor { get; }
    public IoEventLogViewModel IoEventLog { get; }
    public AxisParameterEditorViewModel AxisParameterEditor { get; }
    public AlarmViewModel Alarm { get; }

    public ICommand EmergencyStopCommand { get; }
    public ICommand ClearEmergencyStopCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _systemAppService.InitializeAsync(cancellationToken);
        if (AxisMonitor.SelectedAxis is not null)
        {
            await HandleSelectedAxisChangedAsync(AxisMonitor.SelectedAxis);
        }
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

        if (force || now - _lastIoRefreshUtc >= TimeSpan.FromMilliseconds(300))
        {
            IoMonitor.RefreshAll();
            _lastIoRefreshUtc = now;
        }

        if (force || now - _lastIoEventRefreshUtc >= TimeSpan.FromMilliseconds(500))
        {
            IoEventLog.Refresh();
            _lastIoEventRefreshUtc = now;
        }
    }

    private async Task HandleSelectedAxisChangedAsync(AxisViewModel? axis)
    {
        if (axis is null)
        {
            return;
        }

        AxisDebug.SelectedAxisNo = axis.AxisNo;
        await AxisParameterEditor.SyncAxisNoAsync(axis.AxisNo);
    }
}
