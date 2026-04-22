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

    public string Name { get => _workHead.Name; set { if (_workHead.Name == value) return; _workHead.UpdateMetadata(value, _workHead.Description, _workHead.XAxisNo, _workHead.YAxisNo, _workHead.ZAxisNo, _workHead.RAxisNo, _workHead.VacuumOutputAddress, _workHead.BlowOutputAddress, _workHead.VacuumInputAddress); OnPropertyChanged(); } }
    public string Description { get => _workHead.Description; set { if (_workHead.Description == value) return; _workHead.UpdateMetadata(_workHead.Name, value, _workHead.XAxisNo, _workHead.YAxisNo, _workHead.ZAxisNo, _workHead.RAxisNo, _workHead.VacuumOutputAddress, _workHead.BlowOutputAddress, _workHead.VacuumInputAddress); OnPropertyChanged(); } }
    public int XAxisNo { get => _workHead.XAxisNo; set { if (_workHead.XAxisNo == value) return; _workHead.UpdateMetadata(_workHead.Name, _workHead.Description, value, _workHead.YAxisNo, _workHead.ZAxisNo, _workHead.RAxisNo, _workHead.VacuumOutputAddress, _workHead.BlowOutputAddress, _workHead.VacuumInputAddress); OnPropertyChanged(); } }
    public int YAxisNo { get => _workHead.YAxisNo; set { if (_workHead.YAxisNo == value) return; _workHead.UpdateMetadata(_workHead.Name, _workHead.Description, _workHead.XAxisNo, value, _workHead.ZAxisNo, _workHead.RAxisNo, _workHead.VacuumOutputAddress, _workHead.BlowOutputAddress, _workHead.VacuumInputAddress); OnPropertyChanged(); } }
    public int ZAxisNo { get => _workHead.ZAxisNo; set { if (_workHead.ZAxisNo == value) return; _workHead.UpdateMetadata(_workHead.Name, _workHead.Description, _workHead.XAxisNo, _workHead.YAxisNo, value, _workHead.RAxisNo, _workHead.VacuumOutputAddress, _workHead.BlowOutputAddress, _workHead.VacuumInputAddress); OnPropertyChanged(); } }
    public int RAxisNo { get => _workHead.RAxisNo; set { if (_workHead.RAxisNo == value) return; _workHead.UpdateMetadata(_workHead.Name, _workHead.Description, _workHead.XAxisNo, _workHead.YAxisNo, _workHead.ZAxisNo, value, _workHead.VacuumOutputAddress, _workHead.BlowOutputAddress, _workHead.VacuumInputAddress); OnPropertyChanged(); } }
    public int VacuumOutputAddress { get => _workHead.VacuumOutputAddress; set { if (_workHead.VacuumOutputAddress == value) return; _workHead.UpdateMetadata(_workHead.Name, _workHead.Description, _workHead.XAxisNo, _workHead.YAxisNo, _workHead.ZAxisNo, _workHead.RAxisNo, value, _workHead.BlowOutputAddress, _workHead.VacuumInputAddress); OnPropertyChanged(); } }
    public int BlowOutputAddress { get => _workHead.BlowOutputAddress; set { if (_workHead.BlowOutputAddress == value) return; _workHead.UpdateMetadata(_workHead.Name, _workHead.Description, _workHead.XAxisNo, _workHead.YAxisNo, _workHead.ZAxisNo, _workHead.RAxisNo, _workHead.VacuumOutputAddress, value, _workHead.VacuumInputAddress); OnPropertyChanged(); } }
    public int VacuumInputAddress { get => _workHead.VacuumInputAddress; set { if (_workHead.VacuumInputAddress == value) return; _workHead.UpdateMetadata(_workHead.Name, _workHead.Description, _workHead.XAxisNo, _workHead.YAxisNo, _workHead.ZAxisNo, _workHead.RAxisNo, _workHead.VacuumOutputAddress, _workHead.BlowOutputAddress, value); OnPropertyChanged(); } }

    public bool VacuumDoOn { get; private set; }
    public bool BlowDoOn { get; private set; }
    public bool VacuumDiOn { get; private set; }
    public Brush VacuumDoBrush => VacuumDoOn ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public Brush BlowDoBrush => BlowDoOn ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public Brush VacuumDiBrush => VacuumDiOn ? new SolidColorBrush(Color.FromRgb(0, 200, 0)) : new SolidColorBrush(Color.FromRgb(100, 100, 100));
    public ICommand VacuumCommand { get; }
    public ICommand BlowCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        VacuumDoOn = _machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == _workHead.VacuumOutputAddress)?.Value ?? false;
        BlowDoOn = _machine.IoPoints.FirstOrDefault(io => io.IsOutput && io.Address == _workHead.BlowOutputAddress)?.Value ?? false;
        VacuumDiOn = _machine.IoPoints.FirstOrDefault(io => !io.IsOutput && io.Address == _workHead.VacuumInputAddress)?.Value ?? false;
        OnPropertyChanged(nameof(VacuumDoOn)); OnPropertyChanged(nameof(BlowDoOn)); OnPropertyChanged(nameof(VacuumDiOn));
        OnPropertyChanged(nameof(VacuumDoBrush)); OnPropertyChanged(nameof(BlowDoBrush)); OnPropertyChanged(nameof(VacuumDiBrush));
        OnPropertyChanged(nameof(Name)); OnPropertyChanged(nameof(Description)); OnPropertyChanged(nameof(XAxisNo)); OnPropertyChanged(nameof(YAxisNo)); OnPropertyChanged(nameof(ZAxisNo)); OnPropertyChanged(nameof(RAxisNo));
        OnPropertyChanged(nameof(VacuumOutputAddress)); OnPropertyChanged(nameof(BlowOutputAddress)); OnPropertyChanged(nameof(VacuumInputAddress));
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
