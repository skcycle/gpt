using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MotionControl.Domain.Enums;
using MotionControl.Application.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IOperationStatusReporter
{
    private readonly Machine _machine;
    private readonly IAxisManagementAppService _axisManagementAppService;
    private readonly IIoManagementAppService _ioManagementAppService;
    private readonly ICylinderManagementAppService _cylinderManagementAppService;
    private readonly ISystemAppService _systemAppService;
    private readonly AxisConsoleCoordinator _axisConsoleCoordinator;
    private readonly IoMonitorCoordinator _ioMonitorCoordinator;
    private readonly CommandFeedbackRuntimeState _commandFeedbackRuntimeState;
    private readonly CylinderEventRuntimeState _cylinderEventRuntimeState;
    private readonly ControllerRuntimeState _controllerRuntimeState;
    private readonly Timer _clockTimer;
    private DateTime _lastDashboardRefreshUtc = DateTime.MinValue;
    private DateTime _lastAxisRefreshUtc = DateTime.MinValue;
    private DateTime _lastAlarmRefreshUtc = DateTime.MinValue;
    private DateTime _lastIoRefreshUtc = DateTime.MinValue;
    private DateTime _lastIoEventRefreshUtc = DateTime.MinValue;
    private DateTime _lastCylinderRefreshUtc = DateTime.MinValue;
    private string _currentBeijingTime = GetBeijingTimeString();
    private string _operationStatus = "Ready";

    public MainWindowViewModel(
        Machine machine,
        ISystemAppService systemAppService,
        IMotionAppService motionAppService,
        IAxisManagementAppService axisManagementAppService,
        IAxisControllerParameterAppService axisControllerParameterAppService,
        IIoManagementAppService ioManagementAppService,
        ICylinderManagementAppService cylinderManagementAppService,
        ControllerRuntimeState controllerRuntimeState,
        HomePlanRuntimeState homePlanRuntimeState,
        CommandFeedbackRuntimeState commandFeedbackRuntimeState,
        IoEventRuntimeState ioEventRuntimeState,
        CylinderEventRuntimeState cylinderEventRuntimeState,
        IoControlService ioControlService)
    {
        _machine = machine;
        _axisManagementAppService = axisManagementAppService;
        _ioManagementAppService = ioManagementAppService;
        _cylinderManagementAppService = cylinderManagementAppService;
        _commandFeedbackRuntimeState = commandFeedbackRuntimeState;
        _cylinderEventRuntimeState = cylinderEventRuntimeState;
        _commandFeedbackRuntimeState.FeedbackChanged += () => RefreshViewModels(force: true);
        _systemAppService = systemAppService;
        _controllerRuntimeState = controllerRuntimeState;
        Dashboard = new DashboardViewModel(machine, commandFeedbackRuntimeState);
        EtherCatMonitor = new EtherCatMonitorViewModel(Dashboard);
        AxisMonitor = new AxisMonitorViewModel(machine);
        AxisMonitor.SelectedAxisChanged += _ => RaiseAxisDeleteCanExecuteChanged();
        IoMonitor = new IoMonitorViewModel(machine, ioControlService, CanWriteIoOutputs);
        IoEventLog = new IoEventLogViewModel(ioEventRuntimeState);
        CylinderEventLog = new CylinderEventLogViewModel(cylinderEventRuntimeState);
        CylinderMonitor = new CylinderMonitorViewModel(machine, ioControlService, cylinderEventRuntimeState, CanWriteIoOutputs);
        CylinderMonitor.SelectedCylinderChanged += _ => (DeleteCylinderCommand as RelayCommand)?.RaiseCanExecuteChanged();
        AxisDebug = new AxisDebugViewModel(motionAppService, machine, homePlanRuntimeState, CanControlAxisCommands);
        AxisParameterEditor = new AxisParameterEditorViewModel(
            axisManagementAppService,
            axisControllerParameterAppService,
            CanWriteAxisConfiguration,
            CanAccessControllerParameters,
            this);
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
        AddInputCommand = new RelayCommand(async () => await AddIoPointAsync(false), CanEditIoConfiguration);
        AddOutputCommand = new RelayCommand(async () => await AddIoPointAsync(true), CanEditIoConfiguration);
        DeleteInputCommand = new RelayCommand(async () => await DeleteSelectedInputAsync(), () => IoMonitor.SelectedInput is not null && CanEditIoConfiguration());
        DeleteOutputCommand = new RelayCommand(async () => await DeleteSelectedOutputAsync(), () => IoMonitor.SelectedOutput is not null && CanEditIoConfiguration());
        SaveIoConfigCommand = new RelayCommand(async () => await SaveIoConfigAsync(), CanEditIoConfiguration);
        LoadIoConfigCommand = new RelayCommand(async () => await LoadIoConfigAsync(), CanEditIoConfiguration);
        AddCylinderCommand = new RelayCommand(async () => await AddCylinderAsync(), CanEditIoConfiguration);
        DeleteCylinderCommand = new RelayCommand(async () => await DeleteSelectedCylinderAsync(), () => CylinderMonitor.SelectedCylinder is not null && CanEditIoConfiguration());
        SaveCylinderConfigCommand = new RelayCommand(async () => await SaveCylinderConfigAsync(), CanEditIoConfiguration);
        LoadCylinderConfigCommand = new RelayCommand(async () => await LoadCylinderConfigAsync(), CanEditIoConfiguration);
        _axisConsoleCoordinator = new AxisConsoleCoordinator(AxisMonitor, AxisDebug, AxisParameterEditor);
        _ioMonitorCoordinator = new IoMonitorCoordinator(IoMonitor, (RelayCommand)DeleteInputCommand, (RelayCommand)DeleteOutputCommand);
        _ioMonitorCoordinator.Initialize();
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
    public string OperationStatus
    {
        get => _operationStatus;
        private set
        {
            if (_operationStatus == value)
            {
                return;
            }

            _operationStatus = value;
            OnPropertyChanged();
        }
    }

    public EtherCatMonitorViewModel EtherCatMonitor { get; }
    public AxisMonitorViewModel AxisMonitor { get; }
    public AxisDebugViewModel AxisDebug { get; }
    public IoMonitorViewModel IoMonitor { get; }
    public IoEventLogViewModel IoEventLog { get; }
    public CylinderMonitorViewModel CylinderMonitor { get; }
    public CylinderEventLogViewModel CylinderEventLog { get; }
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
    public ICommand AddCylinderCommand { get; }
    public ICommand DeleteCylinderCommand { get; }
    public ICommand SaveCylinderConfigCommand { get; }
    public ICommand LoadCylinderConfigCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _systemAppService.InitializeAsync(cancellationToken);
        await _axisConsoleCoordinator.InitializeAsync(cancellationToken);
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
        AxisDebug.RefreshCommandStates();
        AxisParameterEditor.RefreshCommandStates();
        RaiseAxisDeleteCanExecuteChanged();
        (AddInputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AddOutputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteInputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteOutputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SaveIoConfigCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (LoadIoConfigCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AddCylinderCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteCylinderCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SaveCylinderConfigCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (LoadCylinderConfigCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        if (force || now - _lastCylinderRefreshUtc >= TimeSpan.FromMilliseconds(300))
        {
            RefreshCylinderStates();
            CylinderMonitor.RefreshAll();
            CylinderEventLog.Refresh();
            _lastCylinderRefreshUtc = now;
        }
    }

    private void RaiseAxisDeleteCanExecuteChanged()
    {
        (DeleteAxisCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task AddAxisAsync()
    {
        var item = await _axisManagementAppService.AddAxisAsync();
        var axis = _machine.Axes.FirstOrDefault(a => a.Id.Value == item.AxisNo);
        if (axis is null)
        {
            OperationStatus = $"Axis {item.AxisNo} 创建后未同步到运行时";
            return;
        }

        AxisMonitor.AddAxis(axis);
        RaiseAxisDeleteCanExecuteChanged();
        await _axisConsoleCoordinator.SyncSelectedAxisAsync(item.AxisNo);
        OperationStatus = $"Axis {item.AxisNo} 已新增";
        RefreshViewModels(force: true);
    }

    private async Task DeleteSelectedAxisAsync()
    {
        var selectedAxis = AxisMonitor.SelectedAxis;
        if (selectedAxis is null)
        {
            return;
        }

        if (!UiGuards.Confirm("删除 Axis", $"确定删除 Axis {selectedAxis.AxisNo} 吗？此操作会同时更新配置。"))
        {
            OperationStatus = "已取消删除 Axis";
            return;
        }

        var axisNo = selectedAxis.AxisNo;
        var removed = await _axisManagementAppService.DeleteAxisAsync(axisNo);
        if (!removed)
        {
            OperationStatus = $"Axis {axisNo} 删除失败";
            return;
        }

        _machine.RemoveAxis(axisNo);
        AxisMonitor.RemoveAxis(axisNo);
        RaiseAxisDeleteCanExecuteChanged();
        OperationStatus = $"Axis {axisNo} 已删除";
        RefreshViewModels(force: true);
    }

    private async Task AddIoPointAsync(bool isOutput)
    {
        var item = await _ioManagementAppService.AddIoPointAsync(isOutput);
        var ioPoint = _machine.IoPoints.First(io => io.IsOutput == item.IsOutput && io.Address == item.Address);
        IoMonitor.AddIoPoint(ioPoint);
        OperationStatus = $"{(isOutput ? "DO" : "DI")} {item.Address} 已新增";
        RefreshViewModels(force: true);
    }

    private async Task DeleteSelectedInputAsync()
    {
        var selected = IoMonitor.SelectedInput;
        if (selected is null)
        {
            return;
        }

        if (!UiGuards.Confirm("删除输入点", $"确定删除 DI {selected.Address} 吗？此操作会同时更新配置。"))
        {
            OperationStatus = "已取消删除 DI";
            return;
        }

        var removed = await _ioManagementAppService.DeleteIoPointAsync(false, selected.Address);
        if (!removed)
        {
            OperationStatus = $"DI {selected.Address} 删除失败";
            return;
        }
        _ioMonitorCoordinator.AfterDelete(false, selected.Address);
        OperationStatus = $"DI {selected.Address} 已删除";
        RefreshViewModels(force: true);
    }

    private async Task DeleteSelectedOutputAsync()
    {
        var selected = IoMonitor.SelectedOutput;
        if (selected is null)
        {
            return;
        }

        if (!UiGuards.Confirm("删除输出点", $"确定删除 DO {selected.Address} 吗？此操作会同时更新配置。"))
        {
            OperationStatus = "已取消删除 DO";
            return;
        }

        var removed = await _ioManagementAppService.DeleteIoPointAsync(true, selected.Address);
        if (!removed)
        {
            OperationStatus = $"DO {selected.Address} 删除失败";
            return;
        }
        _ioMonitorCoordinator.AfterDelete(true, selected.Address);
        OperationStatus = $"DO {selected.Address} 已删除";
        RefreshViewModels(force: true);
    }

    private async Task AddCylinderAsync()
    {
        var item = await _cylinderManagementAppService.AddCylinderAsync();
        var cylinder = _machine.Cylinders.FirstOrDefault(c => string.Equals(c.Name, item.Name, StringComparison.OrdinalIgnoreCase));
        if (cylinder is null)
        {
            OperationStatus = $"Cylinder {item.Name} 创建后未同步到运行时";
            return;
        }

        CylinderMonitor.AddCylinder(cylinder);
        OperationStatus = $"Cylinder {item.Name} 已新增";
        RefreshViewModels(force: true);
    }

    private async Task DeleteSelectedCylinderAsync()
    {
        var selected = CylinderMonitor.SelectedCylinder;
        if (selected is null)
        {
            return;
        }

        if (!UiGuards.Confirm("删除 Cylinder", $"确定删除 Cylinder {selected.Name} 吗？此操作会同时更新配置。"))
        {
            OperationStatus = "已取消删除 Cylinder";
            return;
        }

        var removed = await _cylinderManagementAppService.DeleteCylinderAsync(selected.Name);
        if (!removed)
        {
            OperationStatus = $"Cylinder {selected.Name} 删除失败";
            return;
        }

        CylinderMonitor.RemoveCylinder(selected.Name);
        OperationStatus = $"Cylinder {selected.Name} 已删除";
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

        var duplicateAddress = items
            .GroupBy(io => new { io.IsOutput, io.Address })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateAddress is not null)
        {
            var msg = $"存在重复地址: {(duplicateAddress.Key.IsOutput ? "DO" : "DI")} {duplicateAddress.Key.Address}";
            OperationStatus = msg;
            System.Windows.MessageBox.Show(msg, "IO 配置校验失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var invalidItem = items.FirstOrDefault(io => string.IsNullOrWhiteSpace(io.Name) || io.Address < 0);
        if (invalidItem is not null)
        {
            var msg = "IO 配置存在空名称或非法地址，保存已取消";
            OperationStatus = msg;
            System.Windows.MessageBox.Show(msg, "IO 配置校验失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (!UiGuards.Confirm("保存 IO 配置", "确定覆盖当前 IO 配置到 appsettings.json 吗？"))
        {
            OperationStatus = "已取消保存 IO 配置";
            return;
        }

        try
        {
            await _ioManagementAppService.SaveIoPointsAsync(items);
            _ioMonitorCoordinator.AfterLoadOrReload();
            OperationStatus = $"IO 配置已保存，共 {items.Count} 个点位";
            RefreshViewModels(force: true);
        }
        catch (InvalidOperationException ex)
        {
            OperationStatus = ex.Message;
            System.Windows.MessageBox.Show(ex.Message, "IO 配置保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
    }

    private async Task SaveCylinderConfigAsync()
    {
        var items = CylinderMonitor.Cylinders
            .Select(cylinder => new MotionControl.Infrastructure.Configuration.CylinderConfigItem
            {
                Name = cylinder.Name,
                Description = cylinder.Description,
                ExtendSensorInputAddress = cylinder.ExtendSensorInputAddress,
                RetractSensorInputAddress = cylinder.RetractSensorInputAddress,
                ExtendOutputAddress = cylinder.ExtendOutputAddress,
                RetractOutputAddress = cylinder.RetractOutputAddress,
                ActionTimeoutMs = cylinder.ActionTimeoutMs
            })
            .ToList();

        try
        {
            await _cylinderManagementAppService.SaveCylindersAsync(items);
            OperationStatus = $"Cylinder 配置已保存，共 {items.Count} 个气缸";
            RefreshViewModels(force: true);
        }
        catch (InvalidOperationException ex)
        {
            OperationStatus = ex.Message;
            System.Windows.MessageBox.Show(ex.Message, "Cylinder 配置保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private async Task LoadCylinderConfigAsync()
    {
        await _cylinderManagementAppService.LoadCylindersAsync();
        OperationStatus = "Cylinder 配置已重新加载";
        RefreshViewModels(force: true);
    }

    private async Task LoadIoConfigAsync()
    {
        if (!UiGuards.Confirm("加载 IO 配置", "确定从 appsettings.json 重新加载 IO 配置吗？未保存修改将丢失。"))
        {
            OperationStatus = "已取消加载 IO 配置";
            return;
        }

        await _ioManagementAppService.LoadIoPointsAsync();
        _ioMonitorCoordinator.AfterLoadOrReload();
        OperationStatus = "IO 配置已重新加载";
        RefreshViewModels(force: true);
    }


    private void RefreshCylinderStates()
    {
        var utcNow = DateTime.UtcNow;
        foreach (var cylinder in _machine.Cylinders)
        {
            var previousState = cylinder.State;
            var previousPendingCommand = cylinder.PendingCommand;
            var extendSensorOn = _machine.IoPoints.FirstOrDefault(io => !io.IsOutput && io.Address == cylinder.ExtendSensorInputAddress)?.Value ?? false;
            var retractSensorOn = _machine.IoPoints.FirstOrDefault(io => !io.IsOutput && io.Address == cylinder.RetractSensorInputAddress)?.Value ?? false;
            var extendOutputOn = _machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == cylinder.ExtendOutputAddress)?.Value ?? false;
            var retractOutputOn = _machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == cylinder.RetractOutputAddress)?.Value ?? false;
            cylinder.UpdateState(extendSensorOn, retractSensorOn, extendOutputOn, retractOutputOn);

            if (previousPendingCommand == CylinderCommandType.Extend && cylinder.State == CylinderState.Extended)
            {
                CylinderEventLogRecord(cylinder.Name, "Success", $"{cylinder.Name} extended successfully");
            }
            else if (previousPendingCommand == CylinderCommandType.Retract && cylinder.State == CylinderState.Retracted)
            {
                CylinderEventLogRecord(cylinder.Name, "Success", $"{cylinder.Name} retracted successfully");
            }
            else if (previousState != CylinderState.Conflict && cylinder.State == CylinderState.Conflict)
            {
                CylinderEventLogRecord(cylinder.Name, "Conflict", $"{cylinder.Name} sensor conflict detected");
            }

            var timeoutAlarmCode = $"CYL-{cylinder.Name}-TIMEOUT";
            if (cylinder.IsActionTimedOut(utcNow))
            {
                if (_machine.UpsertAlarm(timeoutAlarmCode, $"Cylinder {cylinder.Name} {cylinder.PendingCommand} timeout ({cylinder.ActionTimeoutMs} ms)", cylinder.Name, "Cylinder", "Error"))
                {
                    _commandFeedbackRuntimeState.Add(new CommandFeedback
                    {
                        CommandName = "Cylinder",
                        Status = "Timeout",
                        Message = $"{cylinder.Name} {cylinder.PendingCommand} timeout ({cylinder.ActionTimeoutMs} ms)"
                    });
                }
            }
            else if (_machine.ClearAlarm(timeoutAlarmCode))
            {
                _commandFeedbackRuntimeState.Add(new CommandFeedback
                {
                    CommandName = "Cylinder",
                    Status = "Recovered",
                    Message = $"{cylinder.Name} timeout cleared"
                });
            }

            var conflictAlarmCode = $"CYL-{cylinder.Name}-SENSOR-CONFLICT";
            if (cylinder.State == CylinderState.Conflict)
            {
                if (_machine.UpsertAlarm(conflictAlarmCode, $"Cylinder {cylinder.Name} sensor conflict: extend/retract DI both ON", cylinder.Name, "Cylinder", "Error"))
                {
                    _commandFeedbackRuntimeState.Add(new CommandFeedback
                    {
                        CommandName = "Cylinder",
                        Status = "Conflict",
                        Message = $"{cylinder.Name} sensor conflict"
                    });
                }
            }
            else if (_machine.ClearAlarm(conflictAlarmCode))
            {
                _commandFeedbackRuntimeState.Add(new CommandFeedback
                {
                    CommandName = "Cylinder",
                    Status = "Recovered",
                    Message = $"{cylinder.Name} sensor conflict cleared"
                });
            }
        }
    }

    private void CylinderEventLogRecord(string cylinderName, string eventType, string message)
    {
        _cylinderEventRuntimeState.Add(new CylinderEventRecord
        {
            CylinderName = cylinderName,
            EventType = eventType,
            Message = message
        });
    }

    public void ReportStatus(string message)
    {
        OperationStatus = message;
    }

    private bool CanControlAxisCommands()
        => _controllerRuntimeState.IsConnected && _machine.CurrentState != SystemState.EmergencyStop;

    private bool CanWriteAxisConfiguration()
        => AxisMonitor.SelectedAxis is not null && _machine.CurrentState != SystemState.EmergencyStop;

    private bool CanAccessControllerParameters()
        => AxisMonitor.SelectedAxis is not null && _controllerRuntimeState.IsConnected && _machine.CurrentState != SystemState.EmergencyStop;

    private bool CanWriteIoOutputs()
        => _controllerRuntimeState.IsConnected && _machine.CurrentState != SystemState.EmergencyStop;

    private bool CanEditIoConfiguration()
        => _machine.CurrentState != SystemState.EmergencyStop;

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
