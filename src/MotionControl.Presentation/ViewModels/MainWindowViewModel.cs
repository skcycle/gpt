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
    private readonly IAxisManagementAppService _axisManagementAppService;
    private readonly IIoManagementAppService _ioManagementAppService;
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
        IAxisManagementAppService axisManagementAppService,
        IAxisControllerParameterAppService axisControllerParameterAppService,
        IIoManagementAppService ioManagementAppService,
        ControllerRuntimeState controllerRuntimeState,
        HomePlanRuntimeState homePlanRuntimeState,
        CommandFeedbackRuntimeState commandFeedbackRuntimeState,
        IoControlService ioControlService)
    {
        _machine = machine;
        _axisManagementAppService = axisManagementAppService;
        _ioManagementAppService = ioManagementAppService;
        _systemAppService = systemAppService;
        _controllerRuntimeState = controllerRuntimeState;
        Dashboard = new DashboardViewModel(machine, commandFeedbackRuntimeState);
        EtherCatMonitor = new EtherCatMonitorViewModel(Dashboard);
        AxisMonitor = new AxisMonitorViewModel(machine);
        IoMonitor = new IoMonitorViewModel(machine, ioControlService);
        IoMonitor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IoMonitorViewModel.SelectedInput))
            {
                (DeleteInputCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            else if (args.PropertyName == nameof(IoMonitorViewModel.SelectedOutput))
            {
                (DeleteOutputCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        };
        IoEventLog = new IoEventLogViewModel(commandFeedbackRuntimeState);
        AxisDebug = new AxisDebugViewModel(motionAppService, machine, homePlanRuntimeState);
        AxisParameterEditor = new AxisParameterEditorViewModel(axisManagementAppService, axisControllerParameterAppService);
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
        AddInputCommand = new RelayCommand(async () => await AddIoPointAsync(false));
        AddOutputCommand = new RelayCommand(async () => await AddIoPointAsync(true));
        DeleteInputCommand = new RelayCommand(async () => await DeleteSelectedInputAsync(), () => IoMonitor.SelectedInput is not null);
        DeleteOutputCommand = new RelayCommand(async () => await DeleteSelectedOutputAsync(), () => IoMonitor.SelectedOutput is not null);
        SaveIoConfigCommand = new RelayCommand(async () => await SaveIoConfigAsync());
        LoadIoConfigCommand = new RelayCommand(async () => await LoadIoConfigAsync());
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
    public ICommand AddInputCommand { get; }
    public ICommand AddOutputCommand { get; }
    public ICommand DeleteInputCommand { get; }
    public ICommand DeleteOutputCommand { get; }
    public ICommand SaveIoConfigCommand { get; }
    public ICommand LoadIoConfigCommand { get; }

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
        var item = await _axisManagementAppService.AddAxisAsync();
        var axis = _machine.Axes.First(a => a.Id.Value == item.AxisNo);
        AxisMonitor.AddAxis(axis);
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
        var removed = await _axisManagementAppService.DeleteAxisAsync(axisNo);
        if (!removed)
        {
            return;
        }

        _machine.RemoveAxis(axisNo);
        AxisMonitor.RemoveAxis(axisNo);
        RefreshViewModels(force: true);
    }

    private async Task AddIoPointAsync(bool isOutput)
    {
        var item = await _ioManagementAppService.AddIoPointAsync(isOutput);
        var ioPoint = _machine.IoPoints.First(io => io.IsOutput == item.IsOutput && io.Address == item.Address);
        IoMonitor.AddIoPoint(ioPoint);
        RefreshViewModels(force: true);
    }

    private async Task DeleteSelectedInputAsync()
    {
        var selected = IoMonitor.SelectedInput;
        if (selected is null)
        {
            return;
        }

        var removed = await _ioManagementAppService.DeleteIoPointAsync(false, selected.Address);
        if (!removed)
        {
            return;
        }
        IoMonitor.RemoveIoPoint(false, selected.Address);
        (DeleteInputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        RefreshViewModels(force: true);
    }

    private async Task DeleteSelectedOutputAsync()
    {
        var selected = IoMonitor.SelectedOutput;
        if (selected is null)
        {
            return;
        }

        var removed = await _ioManagementAppService.DeleteIoPointAsync(true, selected.Address);
        if (!removed)
        {
            return;
        }
        IoMonitor.RemoveIoPoint(true, selected.Address);
        (DeleteOutputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        RefreshViewModels(force: true);
    }

    private async Task SaveIoConfigAsync()
    {
        var items = IoMonitor.GetAllIoPoints()
            .Select(io => new MotionControl.Infrastructure.Configuration.IoPointConfigItem
            {
                Name = io.Name,
                Address = io.Address,
                IsOutput = io.IsOutput,
                Description = io.Description
            })
            .ToList();

        await _ioManagementAppService.SaveIoPointsAsync(items);
        IoMonitor.ReloadFromMachine();
        RefreshViewModels(force: true);
    }

    private async Task LoadIoConfigAsync()
    {
        await _ioManagementAppService.LoadIoPointsAsync();

        IoMonitor.SelectedInput = null;
        IoMonitor.SelectedOutput = null;
        IoMonitor.ReloadFromMachine();
        (DeleteInputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteOutputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        RefreshViewModels(force: true);
    }

    private static string GetBeijingTimeString()
    {
        var utcNow = DateTime.UtcNow;
        var beijingNow = utcNow + TimeSpan.FromHours(8);
        return beijingNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
