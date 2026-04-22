using System;
using System.Collections.Generic;
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
    private readonly IMotionAppService _motionAppService;
    private readonly IAxisManagementAppService _axisManagementAppService;
    private readonly IIoManagementAppService _ioManagementAppService;
    private readonly ICylinderManagementAppService _cylinderManagementAppService;
    private readonly IWorkHeadManagementAppService _workHeadManagementAppService;
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
    private DateTime _lastWorkHeadRefreshUtc = DateTime.MinValue;
    private string _currentBeijingTime = GetBeijingTimeString();
    private string _operationStatus = "Ready";
    private string? _selectedWorkHeadMotionName;
    private double _workHeadTargetX;
    private double _workHeadTargetY;
    private double _workHeadTargetZ;
    private double _workHeadTargetR;
    private double _workHeadMoveVelocity = 100;
    private double _workHeadMoveAcceleration = 100;
    private double _workHeadMoveDeceleration = 100;
    private string? _selectedWorkHeadPositionName;

    public MainWindowViewModel(
        Machine machine,
        ISystemAppService systemAppService,
        IMotionAppService motionAppService,
        IAxisManagementAppService axisManagementAppService,
        IAxisControllerParameterAppService axisControllerParameterAppService,
        MotionControl.Control.Interfaces.IAxisControlService axisControlService,
        IIoManagementAppService ioManagementAppService,
        ICylinderManagementAppService cylinderManagementAppService,
        IWorkHeadManagementAppService workHeadManagementAppService,
        ControllerRuntimeState controllerRuntimeState,
        HomePlanRuntimeState homePlanRuntimeState,
        CommandFeedbackRuntimeState commandFeedbackRuntimeState,
        IoEventRuntimeState ioEventRuntimeState,
        CylinderEventRuntimeState cylinderEventRuntimeState,
        WorkHeadEventRuntimeState workHeadEventRuntimeState,
        IoControlService ioControlService)
    {
        _machine = machine;
        _motionAppService = motionAppService;
        _axisManagementAppService = axisManagementAppService;
        _ioManagementAppService = ioManagementAppService;
        _cylinderManagementAppService = cylinderManagementAppService;
        _workHeadManagementAppService = workHeadManagementAppService;
        _commandFeedbackRuntimeState = commandFeedbackRuntimeState;
        _cylinderEventRuntimeState = cylinderEventRuntimeState;
        _commandFeedbackRuntimeState.FeedbackChanged += () => RefreshViewModels(force: true);
        _systemAppService = systemAppService;
        _controllerRuntimeState = controllerRuntimeState;
        Dashboard = new DashboardViewModel(machine, commandFeedbackRuntimeState);
        EtherCatMonitor = new EtherCatMonitorViewModel(Dashboard);
        AxisMonitor = new AxisMonitorViewModel(machine, axisControlService);
        AxisMonitor.SelectedAxisChanged += _ => RaiseAxisDeleteCanExecuteChanged();
        IoMonitor = new IoMonitorViewModel(machine, ioControlService, CanWriteIoOutputs);
        IoEventLog = new IoEventLogViewModel(ioEventRuntimeState);
        CylinderEventLog = new CylinderEventLogViewModel(cylinderEventRuntimeState);
        WorkHeadEventLog = new WorkHeadEventLogViewModel(workHeadEventRuntimeState);
        CylinderMonitor = new CylinderMonitorViewModel(machine, ioControlService, cylinderEventRuntimeState, CanWriteIoOutputs);
        CylinderMonitor.SelectedCylinderChanged += _ => (DeleteCylinderCommand as RelayCommand)?.RaiseCanExecuteChanged();
        WorkHeadMonitor = new WorkHeadMonitorViewModel(machine, ioControlService, motionAppService, workHeadEventRuntimeState, CanWriteIoOutputs);
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
        AddWorkHeadCommand = new RelayCommand(async () => await AddWorkHeadAsync(), CanEditIoConfiguration);
        DeleteWorkHeadCommand = new RelayCommand(async () => await DeleteSelectedWorkHeadAsync(), () => WorkHeadMonitor.SelectedWorkHead is not null && CanEditIoConfiguration());
        SaveWorkHeadConfigCommand = new RelayCommand(async () => await SaveWorkHeadConfigAsync(), CanEditIoConfiguration);
        LoadWorkHeadConfigCommand = new RelayCommand(async () => await LoadWorkHeadConfigAsync(), CanEditIoConfiguration);
        MoveWorkHeadCommand = new RelayCommand(async () => await MoveSelectedWorkHeadAsync(), () => !string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName));
        TeachWorkHeadCommand = new RelayCommand(TeachSelectedWorkHeadPosition, () => !string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName));
        AddWorkHeadPositionCommand = new RelayCommand(AddWorkHeadPosition, () => WorkHeadMonitor.SelectedWorkHead is not null);
        DeleteWorkHeadPositionCommand = new RelayCommand(DeleteWorkHeadPosition, () => WorkHeadMonitor.SelectedWorkHead is not null);
        TeachToWorkHeadPositionCommand = new RelayCommand(TeachToSelectedWorkHeadPosition, () => !string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName) && !string.IsNullOrWhiteSpace(SelectedWorkHeadPositionName));
        MoveToWorkHeadPositionCommand = new RelayCommand(async () => await MoveToSelectedWorkHeadPositionAsync(), () => !string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName) && !string.IsNullOrWhiteSpace(SelectedWorkHeadPositionName));
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

    public IReadOnlyList<string> WorkHeadNames => _machine.WorkHeads.Select(item => item.Name).OrderBy(item => item).ToList();

    public string? SelectedWorkHeadMotionName { get => _selectedWorkHeadMotionName; set { if (_selectedWorkHeadMotionName == value) return; _selectedWorkHeadMotionName = value; OnPropertyChanged(); (MoveWorkHeadCommand as RelayCommand)?.RaiseCanExecuteChanged(); (TeachWorkHeadCommand as RelayCommand)?.RaiseCanExecuteChanged(); (AddWorkHeadPositionCommand as RelayCommand)?.RaiseCanExecuteChanged(); (DeleteWorkHeadPositionCommand as RelayCommand)?.RaiseCanExecuteChanged(); (TeachToWorkHeadPositionCommand as RelayCommand)?.RaiseCanExecuteChanged(); (MoveToWorkHeadPositionCommand as RelayCommand)?.RaiseCanExecuteChanged(); OnPropertyChanged(nameof(WorkHeadPositionNames)); SelectedWorkHeadPositionName = null; } }
    public double WorkHeadTargetX { get => _workHeadTargetX; set { if (_workHeadTargetX == value) return; _workHeadTargetX = value; OnPropertyChanged(); } }
    public double WorkHeadTargetY { get => _workHeadTargetY; set { if (_workHeadTargetY == value) return; _workHeadTargetY = value; OnPropertyChanged(); } }
    public double WorkHeadTargetZ { get => _workHeadTargetZ; set { if (_workHeadTargetZ == value) return; _workHeadTargetZ = value; OnPropertyChanged(); } }
    public double WorkHeadTargetR { get => _workHeadTargetR; set { if (_workHeadTargetR == value) return; _workHeadTargetR = value; OnPropertyChanged(); } }
    public double WorkHeadMoveVelocity { get => _workHeadMoveVelocity; set { if (_workHeadMoveVelocity == value) return; _workHeadMoveVelocity = value; OnPropertyChanged(); } }
    public double WorkHeadMoveAcceleration { get => _workHeadMoveAcceleration; set { if (_workHeadMoveAcceleration == value) return; _workHeadMoveAcceleration = value; OnPropertyChanged(); } }
    public double WorkHeadMoveDeceleration { get => _workHeadMoveDeceleration; set { if (_workHeadMoveDeceleration == value) return; _workHeadMoveDeceleration = value; OnPropertyChanged(); } }

    public string? SelectedWorkHeadPositionName { get => _selectedWorkHeadPositionName; set { if (_selectedWorkHeadPositionName == value) return; _selectedWorkHeadPositionName = value; OnPropertyChanged(); (DeleteWorkHeadPositionCommand as RelayCommand)?.RaiseCanExecuteChanged(); (TeachToWorkHeadPositionCommand as RelayCommand)?.RaiseCanExecuteChanged(); (MoveToWorkHeadPositionCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

    public IReadOnlyList<WorkHeadPosition> GetWorkHeadPositions(string workHeadName) =>
        _machine.WorkHeads.FirstOrDefault(w => string.Equals(w.Name, workHeadName, StringComparison.OrdinalIgnoreCase))?.Positions ?? new List<WorkHeadPosition>();
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
    public WorkHeadMonitorViewModel WorkHeadMonitor { get; }
    public CylinderEventLogViewModel CylinderEventLog { get; }
    public WorkHeadEventLogViewModel WorkHeadEventLog { get; }
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
    public ICommand AddWorkHeadCommand { get; }
    public ICommand DeleteWorkHeadCommand { get; }
    public ICommand SaveWorkHeadConfigCommand { get; }
    public ICommand LoadWorkHeadConfigCommand { get; }
    public ICommand MoveWorkHeadCommand { get; }
    public ICommand TeachWorkHeadCommand { get; }
    public ICommand AddWorkHeadPositionCommand { get; }
    public ICommand DeleteWorkHeadPositionCommand { get; }
    public ICommand TeachToWorkHeadPositionCommand { get; }
    public ICommand MoveToWorkHeadPositionCommand { get; }

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
        (AddWorkHeadCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteWorkHeadCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SaveWorkHeadConfigCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (LoadWorkHeadConfigCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        if (force || now - _lastWorkHeadRefreshUtc >= TimeSpan.FromMilliseconds(300))
        {
            WorkHeadMonitor.RefreshAll();
            WorkHeadEventLog.Refresh();
            _lastWorkHeadRefreshUtc = now;
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

    private void TeachSelectedWorkHeadPosition()
    {
        if (string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName))
        {
            OperationStatus = "请先选择 WorkHead";
            return;
        }

        var workHead = _machine.WorkHeads.FirstOrDefault(item => string.Equals(item.Name, SelectedWorkHeadMotionName, StringComparison.OrdinalIgnoreCase));
        if (workHead is null)
        {
            OperationStatus = $"未找到 WorkHead: {SelectedWorkHeadMotionName}";
            return;
        }

        if (workHead.XAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == workHead.XAxisNo);
            if (axis is not null) WorkHeadTargetX = axis.CurrentPosition;
        }
        if (workHead.YAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == workHead.YAxisNo);
            if (axis is not null) WorkHeadTargetY = axis.CurrentPosition;
        }
        if (workHead.ZAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == workHead.ZAxisNo);
            if (axis is not null) WorkHeadTargetZ = axis.CurrentPosition;
        }
        if (workHead.RAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == workHead.RAxisNo);
            if (axis is not null) WorkHeadTargetR = axis.CurrentPosition;
        }

        OperationStatus = $"WorkHead {workHead.Name} 当前坐标已 Teach";
    }

    private void AddWorkHeadPosition()
    {
        if (WorkHeadMonitor.SelectedWorkHead is null) return;
        WorkHeadMonitor.SelectedWorkHead.AddPosition();
        OperationStatus = $"{WorkHeadMonitor.SelectedWorkHead.Name} 已添加 Position";
    }

    private void DeleteWorkHeadPosition()
    {
        if (WorkHeadMonitor.SelectedWorkHead is null) return;
        WorkHeadMonitor.SelectedWorkHead.DeleteSelectedPosition();
        OperationStatus = $"{WorkHeadMonitor.SelectedWorkHead.Name} 已删除 Position";
    }

    private void TeachToSelectedWorkHeadPosition()
    {
        if (string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName) || string.IsNullOrWhiteSpace(SelectedWorkHeadPositionName)) return;
        var workHead = _machine.WorkHeads.FirstOrDefault(w => string.Equals(w.Name, SelectedWorkHeadMotionName, StringComparison.OrdinalIgnoreCase));
        if (workHead is null) return;

        var pos = workHead.Positions.FirstOrDefault(p => p.Name == SelectedWorkHeadPositionName);
        if (pos is null) return;

        if (workHead.XAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == workHead.XAxisNo);
            if (axis is not null) pos.X = axis.CurrentPosition;
        }
        if (workHead.YAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == workHead.YAxisNo);
            if (axis is not null) pos.Y = axis.CurrentPosition;
        }
        if (workHead.ZAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == workHead.ZAxisNo);
            if (axis is not null) pos.Z = axis.CurrentPosition;
        }
        if (workHead.RAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == workHead.RAxisNo);
            if (axis is not null) pos.R = axis.CurrentPosition;
        }

        OnPropertyChanged(nameof(WorkHeadPositionNames));
        OperationStatus = $"位置 {pos.Name} 坐标已更新";
    }

    private async Task MoveToSelectedWorkHeadPositionAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName) || string.IsNullOrWhiteSpace(SelectedWorkHeadPositionName)) return;
        var workHead = _machine.WorkHeads.FirstOrDefault(w => string.Equals(w.Name, SelectedWorkHeadMotionName, StringComparison.OrdinalIgnoreCase));
        if (workHead is null) return;

        var pos = workHead.Positions.FirstOrDefault(p => p.Name == SelectedWorkHeadPositionName);
        if (pos is null) return;

        // Temporarily set targets and call the existing move logic
        var savedX = WorkHeadTargetX;
        var savedY = WorkHeadTargetY;
        var savedZ = WorkHeadTargetZ;
        var savedR = WorkHeadTargetR;

        WorkHeadTargetX = pos.X;
        WorkHeadTargetY = pos.Y;
        WorkHeadTargetZ = pos.Z;
        WorkHeadTargetR = pos.R;

        await MoveSelectedWorkHeadAsync();

        WorkHeadTargetX = savedX;
        WorkHeadTargetY = savedY;
        WorkHeadTargetZ = savedZ;
        WorkHeadTargetR = savedR;
    }

    public IReadOnlyList<string> WorkHeadPositionNames
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName)) return Array.Empty<string>();
            var workHead = _machine.WorkHeads.FirstOrDefault(w => string.Equals(w.Name, SelectedWorkHeadMotionName, StringComparison.OrdinalIgnoreCase));
            return workHead?.Positions.Select(p => p.Name).ToList() ?? new List<string>();
        }
    }

    private async Task MoveSelectedWorkHeadAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedWorkHeadMotionName))
        {
            OperationStatus = "请先选择 WorkHead";
            return;
        }

        var workHead = _machine.WorkHeads.FirstOrDefault(item => string.Equals(item.Name, SelectedWorkHeadMotionName, StringComparison.OrdinalIgnoreCase));
        if (workHead is null)
        {
            OperationStatus = $"未找到 WorkHead: {SelectedWorkHeadMotionName}";
            return;
        }

        var hasAnyAxis = workHead.XAxisNo >= 0 || workHead.YAxisNo >= 0 || workHead.ZAxisNo >= 0 || workHead.RAxisNo >= 0;
        if (!hasAnyAxis)
        {
            OperationStatus = $"WorkHead {workHead.Name} 没有可运动的轴";
            return;
        }

        // 1. 先抬 Z 到安全位 0
        if (workHead.ZAxisNo >= 0)
        {
            await _motionAppService.MoveAbsoluteAsync(new MotionControl.Application.DTOs.MoveAxisCommandDto(
                workHead.ZAxisNo,
                workHead.SafeZ,
                WorkHeadMoveVelocity,
                WorkHeadMoveAcceleration,
                WorkHeadMoveDeceleration));
        }

        // 2. 再运动 X / Y / R
        var planarMoves = new List<Task>();
        if (workHead.XAxisNo >= 0)
        {
            planarMoves.Add(_motionAppService.MoveAbsoluteAsync(new MotionControl.Application.DTOs.MoveAxisCommandDto(
                workHead.XAxisNo,
                WorkHeadTargetX,
                WorkHeadMoveVelocity,
                WorkHeadMoveAcceleration,
                WorkHeadMoveDeceleration)));
        }
        if (workHead.YAxisNo >= 0)
        {
            planarMoves.Add(_motionAppService.MoveAbsoluteAsync(new MotionControl.Application.DTOs.MoveAxisCommandDto(
                workHead.YAxisNo,
                WorkHeadTargetY,
                WorkHeadMoveVelocity,
                WorkHeadMoveAcceleration,
                WorkHeadMoveDeceleration)));
        }
        if (workHead.RAxisNo >= 0)
        {
            planarMoves.Add(_motionAppService.MoveAbsoluteAsync(new MotionControl.Application.DTOs.MoveAxisCommandDto(
                workHead.RAxisNo,
                WorkHeadTargetR,
                WorkHeadMoveVelocity,
                WorkHeadMoveAcceleration,
                WorkHeadMoveDeceleration)));
        }

        if (planarMoves.Count > 0)
        {
            await Task.WhenAll(planarMoves);
        }

        // 3. 最后 Z 下到目标位
        if (workHead.ZAxisNo >= 0)
        {
            await _motionAppService.MoveAbsoluteAsync(new MotionControl.Application.DTOs.MoveAxisCommandDto(
                workHead.ZAxisNo,
                WorkHeadTargetZ,
                WorkHeadMoveVelocity,
                WorkHeadMoveAcceleration,
                WorkHeadMoveDeceleration));
        }

        OperationStatus = $"WorkHead {workHead.Name} 已执行定位";
    }

    private async Task AddWorkHeadAsync()
    {
        var item = await _workHeadManagementAppService.AddWorkHeadAsync();
        var workHead = _machine.WorkHeads.FirstOrDefault(c => string.Equals(c.Name, item.Name, StringComparison.OrdinalIgnoreCase));
        if (workHead is null)
        {
            OperationStatus = $"WorkHead {item.Name} 创建后未同步到运行时";
            return;
        }

        WorkHeadMonitor.AddWorkHead(workHead);
        OnPropertyChanged(nameof(WorkHeadNames));
        OperationStatus = $"WorkHead {item.Name} 已新增";
        RefreshViewModels(force: true);
    }

    private async Task DeleteSelectedWorkHeadAsync()
    {
        var selected = WorkHeadMonitor.SelectedWorkHead;
        if (selected is null) return;
        if (!UiGuards.Confirm("删除 WorkHead", $"确定删除 WorkHead {selected.Name} 吗？此操作会同时更新配置。"))
        {
            OperationStatus = "已取消删除 WorkHead";
            return;
        }

        var removed = await _workHeadManagementAppService.DeleteWorkHeadAsync(selected.Name);
        if (!removed)
        {
            OperationStatus = $"WorkHead {selected.Name} 删除失败";
            return;
        }

        WorkHeadMonitor.RemoveWorkHead(selected.Name);
        OnPropertyChanged(nameof(WorkHeadNames));
        OperationStatus = $"WorkHead {selected.Name} 已删除";
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

    private async Task SaveWorkHeadConfigAsync()
    {
        var items = WorkHeadMonitor.WorkHeads.Select(workHead => new MotionControl.Infrastructure.Configuration.WorkHeadConfigItem
        {
            Name = workHead.Name,
            Description = workHead.Description,
            XAxisNo = workHead.XAxisNo,
            YAxisNo = workHead.YAxisNo,
            ZAxisNo = workHead.ZAxisNo,
            RAxisNo = workHead.RAxisNo,
            VacuumOutputAddress = workHead.VacuumOutputAddress,
            BlowOutputAddress = workHead.BlowOutputAddress,
            VacuumInputAddress = workHead.VacuumInputAddress,
            GeneralOutputAddress1 = workHead.GeneralOutputAddress1,
            GeneralOutputAddress2 = workHead.GeneralOutputAddress2,
            GeneralInputAddress1 = workHead.GeneralInputAddress1,
            GeneralInputAddress2 = workHead.GeneralInputAddress2,
            VacuumTimeoutMs = workHead.VacuumTimeoutMs,
            SafeZ = workHead.SafeZ,
            Positions = workHead.Positions.Select(p => new MotionControl.Infrastructure.Configuration.WorkHeadPositionConfigItem
            {
                Name = p.Name,
                Description = p.Description,
                X = p.X,
                Y = p.Y,
                Z = p.Z,
                R = p.R
            }).ToList()
        }).ToList();

        if (!UiGuards.Confirm("保存 WorkHead 配置", "确定覆盖当前 WorkHead 配置到 appsettings.json 吗？"))
        {
            OperationStatus = "已取消保存 WorkHead 配置";
            return;
        }

        try
        {
            await _workHeadManagementAppService.SaveWorkHeadsAsync(items);
            OperationStatus = $"WorkHead 配置已保存，共 {items.Count} 个工作头";
            RefreshViewModels(force: true);
        }
        catch (InvalidOperationException ex)
        {
            OperationStatus = ex.Message;
            System.Windows.MessageBox.Show(ex.Message, "WorkHead 配置保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private async Task LoadWorkHeadConfigAsync()
    {
        if (!UiGuards.Confirm("加载 WorkHead 配置", "确定从 appsettings.json 重新加载 WorkHead 配置吗？未保存修改将丢失。"))
        {
            OperationStatus = "已取消加载 WorkHead 配置";
            return;
        }

        await _workHeadManagementAppService.LoadWorkHeadsAsync();
        OnPropertyChanged(nameof(WorkHeadNames));
        OperationStatus = "WorkHead 配置已重新加载";
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
                    CylinderEventLogRecord(cylinder.Name, "Timeout", $"{cylinder.Name} {cylinder.PendingCommand} timeout ({cylinder.ActionTimeoutMs} ms)");
                }
            }
            else if (_machine.ClearAlarm(timeoutAlarmCode))
            {
                CylinderEventLogRecord(cylinder.Name, "Recovered", $"{cylinder.Name} timeout cleared");
            }

            var conflictAlarmCode = $"CYL-{cylinder.Name}-SENSOR-CONFLICT";
            if (cylinder.State == CylinderState.Conflict)
            {
                if (_machine.UpsertAlarm(conflictAlarmCode, $"Cylinder {cylinder.Name} sensor conflict: extend/retract DI both ON", cylinder.Name, "Cylinder", "Error"))
                {
                    CylinderEventLogRecord(cylinder.Name, "Conflict", $"{cylinder.Name} sensor conflict");
                }
            }
            else if (_machine.ClearAlarm(conflictAlarmCode))
            {
                CylinderEventLogRecord(cylinder.Name, "Recovered", $"{cylinder.Name} sensor conflict cleared");
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
