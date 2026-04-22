using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class WorkHeadItemViewModel : INotifyPropertyChanged
{
    private readonly WorkHead _workHead;
    private readonly Machine _machine;
    private readonly IoControlService _ioControlService;
    private readonly Func<bool> _canControl;
    private readonly WorkHeadEventRuntimeState _workHeadEventRuntimeState;
    public WorkHeadItemViewModel(WorkHead workHead, Machine machine, IoControlService ioControlService, WorkHeadEventRuntimeState workHeadEventRuntimeState, Func<bool> canControl)
    {
        _workHead = workHead;
        _machine = machine;
        _ioControlService = ioControlService;
        _workHeadEventRuntimeState = workHeadEventRuntimeState;
        _canControl = canControl;
        VacuumCommand = new RelayCommand(async () => await ToggleVacuumAsync(), () => _canControl() && _workHead.VacuumOutputAddress >= 0);
        BlowCommand = new RelayCommand(async () => await ToggleBlowAsync(), () => _canControl() && _workHead.BlowOutputAddress >= 0);
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
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
        OnPropertyChanged(nameof(VacuumOutputAddress)); OnPropertyChanged(nameof(BlowOutputAddress)); OnPropertyChanged(nameof(VacuumInputAddress)); OnPropertyChanged(nameof(GeneralOutputAddress1)); OnPropertyChanged(nameof(GeneralOutputAddress2)); OnPropertyChanged(nameof(GeneralInputAddress1)); OnPropertyChanged(nameof(GeneralInputAddress2));
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
        }

        var result = await _ioControlService.SetOutputAsync(_workHead.BlowOutputAddress, next);
        if (!result.Success) return;
        _workHeadEventRuntimeState.Add(new WorkHeadEventRecord { WorkHeadName = _workHead.Name, EventType = "Command", Message = next ? $"{_workHead.Name} blow on" : $"{_workHead.Name} blow off" });
        Refresh();
    }

    private void UpdateMetadata(
        string? name = null,
        string? description = null,
        int? xAxisNo = null,
        int? yAxisNo = null,
        int? zAxisNo = null,
        int? rAxisNo = null,
        int? vacuumOutputAddress = null,
        int? blowOutputAddress = null,
        int? vacuumInputAddress = null,
        int? generalOutputAddress1 = null,
        int? generalOutputAddress2 = null,
        int? generalInputAddress1 = null,
        int? generalInputAddress2 = null)
    {
        _workHead.UpdateMetadata(
            name ?? _workHead.Name,
            description ?? _workHead.Description,
            xAxisNo ?? _workHead.XAxisNo,
            yAxisNo ?? _workHead.YAxisNo,
            zAxisNo ?? _workHead.ZAxisNo,
            rAxisNo ?? _workHead.RAxisNo,
            vacuumOutputAddress ?? _workHead.VacuumOutputAddress,
            blowOutputAddress ?? _workHead.BlowOutputAddress,
            vacuumInputAddress ?? _workHead.VacuumInputAddress,
            generalOutputAddress1 ?? _workHead.GeneralOutputAddress1,
            generalOutputAddress2 ?? _workHead.GeneralOutputAddress2,
            generalInputAddress1 ?? _workHead.GeneralInputAddress1,
            generalInputAddress2 ?? _workHead.GeneralInputAddress2);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
