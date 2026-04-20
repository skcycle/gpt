using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MotionControl.Application.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly Machine _machine;
    private readonly IAxisParameterAppService _axisParameterAppService;
    private readonly IAxisRuntimeParameterSyncService _axisRuntimeParameterSyncService;
    private readonly ISystemAppService _systemAppService;
    private readonly ControllerRuntimeState _controllerRuntimeState;
    private readonly Timer _clockTimer;
    private DateTime _lastDashboardRefreshUtc = DateTime.MinValue;
    private DateTime _lastAxisRefreshUtc = DateTime.MinValue;
    private DateTime _lastAlarmRefreshUtc = DateTime.MinValue;
    private DateTime _lastIoRefreshUtc = DateTime.MinValue;
    private DateTime _lastIoEventRefreshUtc = DateTime.MinValue;
    private string _currentBeijingTime = GetBeijingTimeString();

    public MainWindowViewModel(
        Machine machine,
        ISystemAppService systemAppService,
        IMotionAppService motionAppService,
        IAxisParameterAppService axisParameterAppService,
        IAxisRuntimeParameterSyncService axisRuntimeParameterSyncService,
        IAxisControllerParameterAppService axisControllerParameterAppService,
        ControllerRuntimeState controllerRuntimeState,
        HomePlanRuntimeState homePlanRuntimeState,
        CommandFeedbackRuntimeState commandFeedbackRuntimeState,
        IoControlService ioControlService)
    {
        _machine = machine;
        _axisParameterAppService = axisParameterAppService;
        _axisRuntimeParameterSyncService = axisRuntimeParameterSyncService;
        _systemAppService = systemAppService;
        _controllerRuntimeState = controllerRuntimeState;
        Dashboard = new DashboardViewModel(machine, commandFeedbackRuntimeState);
        EtherCatMonitor = new EtherCatMonitorViewModel(Dashboard);
        AxisMonitor = new AxisMonitorViewModel(machine);
        IoMonitor = new IoMonitorViewModel(machine, ioControlService);
        IoEventLog = new IoEventLogViewModel(commandFeedbackRuntimeState);
        AxisDebug = new AxisDebugViewModel(motionAppService, machine, homePlanRuntimeState);
        AxisParameterEditor = new AxisParameterEditorViewModel(axisParameterAppService, axisRuntimeParameterSyncService, axisControllerParameterAppService);
        AxisMonitor.SelectedAxisChanged += async axis => await HandleSelectedAxisChangedAsync(axis);
        AxisDebug.SelectedAxisChanged += async axisNo => await AxisParameterEditor.SyncAxisNoAsync(axisNo);
        Alarm = new AlarmViewModel(machine);
        EmergencyStopCommand = new RelayCommand(async () => await _systemAppService.EmergencyStopAsync());
        ClearEmergencyStopCommand = new RelayCommand(async () => await _systemAppService.ClearEmergencyStopAsync());
        ReconnectCommand = new RelayCommand(
            async () =>
            {
                await _systemAppService.ReconnectAsync();
                RefreshViewModels(force: true);
            });
        AddAxisCommand = new RelayCommand(async () => await AddAxisAsync());
        DeleteAxisCommand = new RelayCommand(async () => await DeleteSelectedAxisAsync(), () => AxisMonitor.SelectedAxis is not null);
        _clockTimer = new Timer(_ => CurrentBeijingTime = GetBeijingTimeString(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public DashboardViewModel Dashboard { get; }
    public string CurrentBeijingTime
    {
        get => _currentBeijingTime;
        private set
        {
            if (_currentBeijingTime == value)
            {
                return;
            }

            _currentBeijingTime = value;
            OnPropertyChanged();
        }
    }
    public EtherCatMonitorViewModel EtherCatMonitor { get; }
    public AxisMonitorViewModel AxisMonitor { get; }
    public AxisDebugViewModel AxisDebug { get; }
    public IoMonitorViewModel IoMonitor { get; }
    public IoEventLogViewModel IoEventLog { get; }
    public AxisParameterEditorViewModel AxisParameterEditor { get; }
    public AlarmViewModel Alarm { get; }

    public ICommand EmergencyStopCommand { get; }
    public ICommand ClearEmergencyStopCommand { get; }
    public ICommand ReconnectCommand { get; }
    public ICommand AddAxisCommand { get; }
    public ICommand DeleteAxisCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

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

        if (force || now - _lastAxisRefreshUtc >= TimeSpan.FromMilliseconds(300))
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
        (DeleteAxisCommand as RelayCommand)?.RaiseCanExecuteChanged();

        if (axis is null)
        {
            return;
        }

        AxisDebug.SelectedAxisNo = axis.AxisNo;
        await AxisParameterEditor.SyncAxisNoAsync(axis.AxisNo);
    }

    private async Task AddAxisAsync()
    {
        var item = await _axisParameterAppService.AddAxisAsync();

        var axis = new Axis(new AxisId(item.AxisNo), item.Name, item.AxisNo);
        if (item.SoftLimitNegative.HasValue && item.SoftLimitPositive.HasValue)
        {
            axis.SetSoftLimit(new SoftLimit(item.SoftLimitNegative.Value, item.SoftLimitPositive.Value));
        }

        axis.SetHomeMode(item.HomeMode);
        axis.SetServoBinding(item.ServoBinding);
        if (item.WorkVelocity.HasValue) axis.SetWorkVelocity(item.WorkVelocity.Value);
        if (item.SetupVelocity.HasValue) axis.SetSetupVelocity(item.SetupVelocity.Value);
        if (item.PulseEquivalent.HasValue) axis.SetPulseEquivalent(item.PulseEquivalent.Value);

        _machine.AddAxis(axis);
        AxisMonitor.AddAxis(axis);
        await _axisRuntimeParameterSyncService.ApplyAsync(item);
        await AxisParameterEditor.SyncAxisNoAsync(item.AxisNo);
        RefreshViewModels(force: true);
    }

    private async Task DeleteSelectedAxisAsync()
    {
        var selectedAxis = AxisMonitor.SelectedAxis;
        if (selectedAxis is null)
        {
            return;
        }

        var axisNo = selectedAxis.AxisNo;
        var removed = await _axisParameterAppService.DeleteAxisAsync(axisNo);
        if (!removed)
        {
            return;
        }

        _machine.RemoveAxis(axisNo);
        AxisMonitor.RemoveAxis(axisNo);
        RefreshViewModels(force: true);
    }

    private static string GetBeijingTimeString()
    {
        var utcNow = DateTime.UtcNow;
        var beijingNow = utcNow + TimeSpan.FromHours(8);
        return beijingNow.ToString("yyyy-MM-dd HH:mm:ss '北京时间'");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
