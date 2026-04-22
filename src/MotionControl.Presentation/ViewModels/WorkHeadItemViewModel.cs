using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using MotionControl.Application.DTOs;
using MotionControl.Application.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class WorkHeadItemViewModel : INotifyPropertyChanged
{
    private readonly WorkHead _workHead;
    private readonly Machine _machine;
    private readonly IoControlService _ioControlService;
    private readonly IMotionAppService _motionAppService;
    private readonly Func<bool> _canControl;
    private readonly WorkHeadEventRuntimeState _workHeadEventRuntimeState;

    public WorkHeadItemViewModel(WorkHead workHead, Machine machine, IoControlService ioControlService, IMotionAppService motionAppService, WorkHeadEventRuntimeState workHeadEventRuntimeState, Func<bool> canControl)
    {
        _workHead = workHead;
        _machine = machine;
        _ioControlService = ioControlService;
        _motionAppService = motionAppService;
        _workHeadEventRuntimeState = workHeadEventRuntimeState;
        _canControl = canControl;
        VacuumCommand = new RelayCommand(async () => await ToggleVacuumAsync(), () => _canControl() && _workHead.VacuumOutputAddress >= 0);
        BlowCommand = new RelayCommand(async () => await ToggleBlowAsync(), () => _canControl() && _workHead.BlowOutputAddress >= 0);
        TeachPositionCommand = new RelayCommand(TeachSelectedPosition, () => SelectedPosition is not null);
        MovePositionCommand = new RelayCommand(async () => await MoveSelectedPositionAsync(), () => SelectedPosition is not null);
    }

    public string Name { get => _workHead.Name; set { if (_workHead.Name == value) return; UpdateMetadata(name: value); OnPropertyChanged(); } }
    public string Description { get => _workHead.Description; set { if (_workHead.Description == value) return; UpdateMetadata(description: value); OnPropertyChanged(); } }
    public int XAxisNo { get => _workHead.XAxisNo; set { if (_workHead.XAxisNo == value) return; UpdateMetadata(xAxisNo: value); OnPropertyChanged(); } }
    public int YAxisNo { get => _workHead.YAxisNo; set { if (_workHead.YAxisNo == value) return; UpdateMetadata(yAxisNo: value); OnPropertyChanged(); } }
    public int ZAxisNo { get => _workHead.ZAxisNo; set { if (_workHead.ZAxisNo == value) return; UpdateMetadata(zAxisNo: value); OnPropertyChanged(); } }
    public int RAxisNo { get => _workHead.RAxisNo; set { if (_workHead.RAxisNo == value) return; UpdateMetadata(rAxisNo: value); OnPropertyChanged(); } }
    public int VacuumOutputAddress { get => _workHead.VacuumOutputAddress; set { if (_workHead.VacuumOutputAddress == value) return; UpdateMetadata(vacuumOutputAddress: value); OnPropertyChanged(); } }
    public int BlowOutputAddress { get => _workHead.BlowOutputAddress; set { if (_workHead.BlowOutputAddress == value) return; UpdateMetadata(blowOutputAddress: value); OnPropertyChanged(); } }
    public int VacuumInputAddress { get => _workHead.VacuumInputAddress; set { if (_workHead.VacuumInputAddress == value) return; UpdateMetadata(vacuumInputAddress: value); OnPropertyChanged(); } }
    public int GeneralOutputAddress1 { get => _workHead.GeneralOutputAddress1; set { if (_workHead.GeneralOutputAddress1 == value) return; UpdateMetadata(generalOutputAddress1: value); OnPropertyChanged(); } }
    public int GeneralOutputAddress2 { get => _workHead.GeneralOutputAddress2; set { if (_workHead.GeneralOutputAddress2 == value) return; UpdateMetadata(generalOutputAddress2: value); OnPropertyChanged(); } }
    public int GeneralInputAddress1 { get => _workHead.GeneralInputAddress1; set { if (_workHead.GeneralInputAddress1 == value) return; UpdateMetadata(generalInputAddress1: value); OnPropertyChanged(); } }
    public int GeneralInputAddress2 { get => _workHead.GeneralInputAddress2; set { if (_workHead.GeneralInputAddress2 == value) return; UpdateMetadata(generalInputAddress2: value); OnPropertyChanged(); } }
    public int VacuumTimeoutMs { get => _workHead.VacuumTimeoutMs; set { if (_workHead.VacuumTimeoutMs == value) return; UpdateMetadata(vacuumTimeoutMs: value); OnPropertyChanged(); } }
    public double SafeZ { get => _workHead.SafeZ; set { if (_workHead.SafeZ == value) return; UpdateMetadata(safeZ: value); OnPropertyChanged(); } }

    public string PositionName
    {
        get => SelectedPosition?.Name ?? string.Empty;
        set
        {
            if (SelectedPosition is null || SelectedPosition.Name == value) return;
            var old = SelectedPosition.Name;
            SelectedPosition.Name = value;
            _workHead.SelectedPositionName = value;
            RaisePositionChanged();
        }
    }

    public string PositionDescription { get => SelectedPosition?.Description ?? string.Empty; set { if (SelectedPosition is null || SelectedPosition.Description == value) return; SelectedPosition.Description = value; RaisePositionChanged(); } }
    public double PositionX { get => SelectedPosition?.X ?? 0; set { if (SelectedPosition is null || SelectedPosition.X == value) return; SelectedPosition.X = value; RaisePositionChanged(); } }
    public double PositionY { get => SelectedPosition?.Y ?? 0; set { if (SelectedPosition is null || SelectedPosition.Y == value) return; SelectedPosition.Y = value; RaisePositionChanged(); } }
    public double PositionZ { get => SelectedPosition?.Z ?? 0; set { if (SelectedPosition is null || SelectedPosition.Z == value) return; SelectedPosition.Z = value; RaisePositionChanged(); } }
    public double PositionR { get => SelectedPosition?.R ?? 0; set { if (SelectedPosition is null || SelectedPosition.R == value) return; SelectedPosition.R = value; RaisePositionChanged(); } }
    public string PositionSelector
    {
        get => _workHead.SelectedPositionName ?? string.Empty;
        set
        {
            if (_workHead.SelectedPositionName == value) return;
            _workHead.SelectedPositionName = value;
            RaisePositionChanged();
        }
    }

    public bool VacuumDoOn { get; private set; }
    public bool BlowDoOn { get; private set; }
    public bool VacuumDiOn { get; private set; }
    public bool GeneralDo1On { get; private set; }
    public bool GeneralDo2On { get; private set; }
    public bool GeneralDi1On { get; private set; }
    public bool GeneralDi2On { get; private set; }
    public Brush VacuumDoBrush => VacuumDoOn ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public Brush BlowDoBrush => BlowDoOn ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public Brush VacuumDiBrush => VacuumDiOn ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public Brush GeneralDo1Brush => GeneralDo1On ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public Brush GeneralDo2Brush => GeneralDo2On ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public Brush GeneralDi1Brush => GeneralDi1On ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public Brush GeneralDi2Brush => GeneralDi2On ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public ICommand VacuumCommand { get; }
    public ICommand BlowCommand { get; }
    public ICommand TeachPositionCommand { get; }
    public ICommand MovePositionCommand { get; }
    public IEnumerable<string> PositionNames => _workHead.Positions.Select(p => p.Name).ToList();
    private WorkHeadPosition? SelectedPosition => _workHead.Positions.FirstOrDefault(p => string.Equals(p.Name, _workHead.SelectedPositionName, StringComparison.OrdinalIgnoreCase)) ?? _workHead.Positions.FirstOrDefault();

    public event PropertyChangedEventHandler? PropertyChanged;

    public void AddPosition()
    {
        var index = _workHead.Positions.Count + 1;
        var name = $"Pos{index}";
        _workHead.AddPosition(new WorkHeadPosition(name, string.Empty, 0, 0, 0, 0));
        _workHead.SelectedPositionName = name;
        RaisePositionChanged();
    }

    public void DeleteSelectedPosition()
    {
        if (SelectedPosition is null) return;
        var removed = SelectedPosition.Name;
        _workHead.RemovePosition(removed);
        _workHead.SelectedPositionName = _workHead.Positions.FirstOrDefault()?.Name;
        RaisePositionChanged();
    }

    public void Refresh()
    {
        if (string.IsNullOrWhiteSpace(_workHead.SelectedPositionName) && _workHead.Positions.Count > 0)
        {
            _workHead.SelectedPositionName = _workHead.Positions[0].Name;
        }

        VacuumDoOn = _machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == _workHead.VacuumOutputAddress)?.Value ?? false;
        BlowDoOn = _machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == _workHead.BlowOutputAddress)?.Value ?? false;
        VacuumDiOn = _machine.IoPoints.FirstOrDefault(io => !io.IsOutput && io.Address == _workHead.VacuumInputAddress)?.Value ?? false;
        GeneralDo1On = _machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == _workHead.GeneralOutputAddress1)?.Value ?? false;
        GeneralDo2On = _machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == _workHead.GeneralOutputAddress2)?.Value ?? false;
        GeneralDi1On = _machine.IoPoints.FirstOrDefault(io => !io.IsOutput && io.Address == _workHead.GeneralInputAddress1)?.Value ?? false;
        GeneralDi2On = _machine.IoPoints.FirstOrDefault(io => !io.IsOutput && io.Address == _workHead.GeneralInputAddress2)?.Value ?? false;
        OnPropertyChanged(nameof(VacuumDoOn)); OnPropertyChanged(nameof(BlowDoOn)); OnPropertyChanged(nameof(VacuumDiOn)); OnPropertyChanged(nameof(GeneralDo1On)); OnPropertyChanged(nameof(GeneralDo2On)); OnPropertyChanged(nameof(GeneralDi1On)); OnPropertyChanged(nameof(GeneralDi2On));
        OnPropertyChanged(nameof(VacuumDoBrush)); OnPropertyChanged(nameof(BlowDoBrush)); OnPropertyChanged(nameof(VacuumDiBrush)); OnPropertyChanged(nameof(GeneralDo1Brush)); OnPropertyChanged(nameof(GeneralDo2Brush)); OnPropertyChanged(nameof(GeneralDi1Brush)); OnPropertyChanged(nameof(GeneralDi2Brush));
        OnPropertyChanged(nameof(Name)); OnPropertyChanged(nameof(Description)); OnPropertyChanged(nameof(XAxisNo)); OnPropertyChanged(nameof(YAxisNo)); OnPropertyChanged(nameof(ZAxisNo)); OnPropertyChanged(nameof(RAxisNo));
        OnPropertyChanged(nameof(VacuumOutputAddress)); OnPropertyChanged(nameof(BlowOutputAddress)); OnPropertyChanged(nameof(VacuumInputAddress)); OnPropertyChanged(nameof(GeneralOutputAddress1)); OnPropertyChanged(nameof(GeneralOutputAddress2)); OnPropertyChanged(nameof(GeneralInputAddress1)); OnPropertyChanged(nameof(GeneralInputAddress2)); OnPropertyChanged(nameof(VacuumTimeoutMs)); OnPropertyChanged(nameof(SafeZ));
        RaisePositionChanged();
        EvaluateVacuumRuntime();
        (VacuumCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (BlowCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task ToggleVacuumAsync()
    {
        if (_workHead.VacuumOutputAddress < 0) return;
        var next = !(_machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == _workHead.VacuumOutputAddress)?.Value ?? false);
        if (next && _workHead.BlowOutputAddress >= 0)
        {
            var blowOff = await _ioControlService.SetOutputAsync(_workHead.BlowOutputAddress, false);
            if (!blowOff.Success) return;
        }
        var result = await _ioControlService.SetOutputAsync(_workHead.VacuumOutputAddress, next);
        if (!result.Success) return;
        if (next) _workHead.StartVacuumCommand(); else _workHead.StopVacuumCommand();
        _workHeadEventRuntimeState.Add(new WorkHeadEventRecord { WorkHeadName = _workHead.Name, EventType = "Command", Message = next ? $"{_workHead.Name} vacuum on" : $"{_workHead.Name} vacuum off" });
        Refresh();
    }

    private async Task ToggleBlowAsync()
    {
        if (_workHead.BlowOutputAddress < 0) return;
        var next = !(_machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == _workHead.BlowOutputAddress)?.Value ?? false);
        if (next && _workHead.VacuumOutputAddress >= 0)
        {
            var vacuumOff = await _ioControlService.SetOutputAsync(_workHead.VacuumOutputAddress, false);
            if (!vacuumOff.Success) return;
            _workHead.StopVacuumCommand();
        }
        var result = await _ioControlService.SetOutputAsync(_workHead.BlowOutputAddress, next);
        if (!result.Success) return;
        _workHeadEventRuntimeState.Add(new WorkHeadEventRecord { WorkHeadName = _workHead.Name, EventType = "Command", Message = next ? $"{_workHead.Name} blow on" : $"{_workHead.Name} blow off" });
        Refresh();
    }

    private void TeachSelectedPosition()
    {
        if (SelectedPosition is null) return;
        if (_workHead.XAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == _workHead.XAxisNo);
            if (axis is not null) SelectedPosition.X = axis.CurrentPosition;
        }
        if (_workHead.YAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == _workHead.YAxisNo);
            if (axis is not null) SelectedPosition.Y = axis.CurrentPosition;
        }
        if (_workHead.ZAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == _workHead.ZAxisNo);
            if (axis is not null) SelectedPosition.Z = axis.CurrentPosition;
        }
        if (_workHead.RAxisNo >= 0)
        {
            var axis = _machine.Axes.FirstOrDefault(item => item.Id.Value == _workHead.RAxisNo);
            if (axis is not null) SelectedPosition.R = axis.CurrentPosition;
        }
        RaisePositionChanged();
    }

    private async Task MoveSelectedPositionAsync()
    {
        if (SelectedPosition is null) return;
        if (_workHead.ZAxisNo >= 0)
        {
            await _motionAppService.MoveAbsoluteAsync(new MoveAxisCommandDto(_workHead.ZAxisNo, _workHead.SafeZ, 100, 100, 100));
        }
        var planarMoves = new List<Task>();
        if (_workHead.XAxisNo >= 0) planarMoves.Add(_motionAppService.MoveAbsoluteAsync(new MoveAxisCommandDto(_workHead.XAxisNo, SelectedPosition.X, 100, 100, 100)));
        if (_workHead.YAxisNo >= 0) planarMoves.Add(_motionAppService.MoveAbsoluteAsync(new MoveAxisCommandDto(_workHead.YAxisNo, SelectedPosition.Y, 100, 100, 100)));
        if (_workHead.RAxisNo >= 0) planarMoves.Add(_motionAppService.MoveAbsoluteAsync(new MoveAxisCommandDto(_workHead.RAxisNo, SelectedPosition.R, 100, 100, 100)));
        if (planarMoves.Count > 0) await Task.WhenAll(planarMoves);
        if (_workHead.ZAxisNo >= 0)
        {
            await _motionAppService.MoveAbsoluteAsync(new MoveAxisCommandDto(_workHead.ZAxisNo, SelectedPosition.Z, 100, 100, 100));
        }
    }

    private void UpdateMetadata(string? name = null, string? description = null, int? xAxisNo = null, int? yAxisNo = null, int? zAxisNo = null, int? rAxisNo = null, int? vacuumOutputAddress = null, int? blowOutputAddress = null, int? vacuumInputAddress = null, int? generalOutputAddress1 = null, int? generalOutputAddress2 = null, int? generalInputAddress1 = null, int? generalInputAddress2 = null, int? vacuumTimeoutMs = null, double? safeZ = null)
    {
        _workHead.UpdateMetadata(name ?? _workHead.Name, description ?? _workHead.Description, xAxisNo ?? _workHead.XAxisNo, yAxisNo ?? _workHead.YAxisNo, zAxisNo ?? _workHead.ZAxisNo, rAxisNo ?? _workHead.RAxisNo, vacuumOutputAddress ?? _workHead.VacuumOutputAddress, blowOutputAddress ?? _workHead.BlowOutputAddress, vacuumInputAddress ?? _workHead.VacuumInputAddress, generalOutputAddress1 ?? _workHead.GeneralOutputAddress1, generalOutputAddress2 ?? _workHead.GeneralOutputAddress2, generalInputAddress1 ?? _workHead.GeneralInputAddress1, generalInputAddress2 ?? _workHead.GeneralInputAddress2, vacuumTimeoutMs ?? _workHead.VacuumTimeoutMs, safeZ ?? _workHead.SafeZ);
    }

    private void EvaluateVacuumRuntime()
    {
        if (VacuumDoOn && VacuumDiOn && _workHead.PendingVacuumCommand && !_workHead.VacuumSuccessLogged)
        {
            _workHead.VacuumSuccessLogged = true;
            _workHead.StopVacuumCommand();
            _workHeadEventRuntimeState.Add(new WorkHeadEventRecord { WorkHeadName = _workHead.Name, EventType = "Success", Message = $"{_workHead.Name} vacuum detected" });
        }
        if (!VacuumDoOn && VacuumDiOn && !_workHead.VacuumConflictLogged)
        {
            _workHead.VacuumConflictLogged = true;
            _workHeadEventRuntimeState.Add(new WorkHeadEventRecord { WorkHeadName = _workHead.Name, EventType = "Conflict", Message = $"{_workHead.Name} vacuum DI on while vacuum DO off" });
        }
        if (_workHead.HasVacuumTimedOut(DateTime.UtcNow) && !_workHead.VacuumTimeoutLogged)
        {
            _workHead.VacuumTimeoutLogged = true;
            _workHeadEventRuntimeState.Add(new WorkHeadEventRecord { WorkHeadName = _workHead.Name, EventType = "Timeout", Message = $"{_workHead.Name} vacuum timeout" });
        }
    }

    private void RaisePositionChanged()
    {
        OnPropertyChanged(nameof(PositionNames));
        OnPropertyChanged(nameof(PositionSelector));
        OnPropertyChanged(nameof(PositionName));
        OnPropertyChanged(nameof(PositionDescription));
        OnPropertyChanged(nameof(PositionX));
        OnPropertyChanged(nameof(PositionY));
        OnPropertyChanged(nameof(PositionZ));
        OnPropertyChanged(nameof(PositionR));
        (TeachPositionCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MovePositionCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
