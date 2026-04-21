using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class CylinderItemViewModel : INotifyPropertyChanged
{
    private readonly Cylinder _cylinder;
    private readonly IoControlService _ioControlService;
    private readonly Func<bool> _canControl;
    private readonly CylinderEventRuntimeState _cylinderEventRuntimeState;

    public CylinderItemViewModel(Cylinder cylinder, IoControlService ioControlService, CylinderEventRuntimeState cylinderEventRuntimeState, Func<bool> canControl)
    {
        _cylinder = cylinder;
        _ioControlService = ioControlService;
        _canControl = canControl;
        _cylinderEventRuntimeState = cylinderEventRuntimeState;
        OpenCommand = new RelayCommand(async () => await OpenAsync(), () => _canControl() && _cylinder.ExtendOutputAddress >= 0);
        CloseCommand = new RelayCommand(async () => await CloseAsync(), () => _canControl() && _cylinder.RetractOutputAddress >= 0);
    }

    public string Name
    {
        get => _cylinder.Name;
        set
        {
            if (_cylinder.Name == value) return;
            _cylinder.UpdateMetadata(value, _cylinder.ExtendSensorInputAddress, _cylinder.RetractSensorInputAddress, _cylinder.ExtendOutputAddress, _cylinder.RetractOutputAddress, _cylinder.Description, _cylinder.ActionTimeoutMs);
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _cylinder.Description;
        set
        {
            if (_cylinder.Description == value) return;
            _cylinder.UpdateMetadata(_cylinder.Name, _cylinder.ExtendSensorInputAddress, _cylinder.RetractSensorInputAddress, _cylinder.ExtendOutputAddress, _cylinder.RetractOutputAddress, value, _cylinder.ActionTimeoutMs);
            OnPropertyChanged();
        }
    }

    public int ExtendSensorInputAddress
    {
        get => _cylinder.ExtendSensorInputAddress;
        set { if (_cylinder.ExtendSensorInputAddress == value) return; _cylinder.UpdateMetadata(_cylinder.Name, value, _cylinder.RetractSensorInputAddress, _cylinder.ExtendOutputAddress, _cylinder.RetractOutputAddress, _cylinder.Description, _cylinder.ActionTimeoutMs); OnPropertyChanged(); }
    }

    public int RetractSensorInputAddress
    {
        get => _cylinder.RetractSensorInputAddress;
        set { if (_cylinder.RetractSensorInputAddress == value) return; _cylinder.UpdateMetadata(_cylinder.Name, _cylinder.ExtendSensorInputAddress, value, _cylinder.ExtendOutputAddress, _cylinder.RetractOutputAddress, _cylinder.Description, _cylinder.ActionTimeoutMs); OnPropertyChanged(); }
    }

    public int ExtendOutputAddress
    {
        get => _cylinder.ExtendOutputAddress;
        set { if (_cylinder.ExtendOutputAddress == value) return; _cylinder.UpdateMetadata(_cylinder.Name, _cylinder.ExtendSensorInputAddress, _cylinder.RetractSensorInputAddress, value, _cylinder.RetractOutputAddress, _cylinder.Description, _cylinder.ActionTimeoutMs); OnPropertyChanged(); }
    }

    public int RetractOutputAddress
    {
        get => _cylinder.RetractOutputAddress;
        set { if (_cylinder.RetractOutputAddress == value) return; _cylinder.UpdateMetadata(_cylinder.Name, _cylinder.ExtendSensorInputAddress, _cylinder.RetractSensorInputAddress, _cylinder.ExtendOutputAddress, value, _cylinder.Description, _cylinder.ActionTimeoutMs); OnPropertyChanged(); }
    }

    public int ActionTimeoutMs
    {
        get => _cylinder.ActionTimeoutMs;
        set { if (_cylinder.ActionTimeoutMs == value) return; _cylinder.UpdateMetadata(_cylinder.Name, _cylinder.ExtendSensorInputAddress, _cylinder.RetractSensorInputAddress, _cylinder.ExtendOutputAddress, _cylinder.RetractOutputAddress, _cylinder.Description, value); OnPropertyChanged(); }
    }

    public string State => _cylinder.State.ToString();
    public ICommand OpenCommand { get; }
    public ICommand CloseCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(ExtendSensorInputAddress));
        OnPropertyChanged(nameof(RetractSensorInputAddress));
        OnPropertyChanged(nameof(ExtendOutputAddress));
        OnPropertyChanged(nameof(RetractOutputAddress));
        OnPropertyChanged(nameof(ActionTimeoutMs));
        OnPropertyChanged(nameof(State));
        (OpenCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CloseCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task OpenAsync()
    {
        if (_cylinder.ExtendOutputAddress < 0 || _cylinder.RetractOutputAddress < 0)
        {
            return;
        }

        var first = await _ioControlService.SetOutputAsync(ExtendOutputAddress, true);
        var second = await _ioControlService.SetOutputAsync(RetractOutputAddress, false);
        if (first.Success && second.Success)
        {
            _cylinder.StartExtendCommand();
            _cylinderEventRuntimeState.Add(new CylinderEventRecord
            {
                CylinderName = _cylinder.Name,
                EventType = "Command",
                Message = $"{_cylinder.Name} open command sent"
            });
            OnPropertyChanged(nameof(State));
        }
    }

    private async Task CloseAsync()
    {
        if (_cylinder.ExtendOutputAddress < 0 || _cylinder.RetractOutputAddress < 0)
        {
            return;
        }

        var first = await _ioControlService.SetOutputAsync(RetractOutputAddress, true);
        var second = await _ioControlService.SetOutputAsync(ExtendOutputAddress, false);
        if (first.Success && second.Success)
        {
            _cylinder.StartRetractCommand();
            _cylinderEventRuntimeState.Add(new CylinderEventRecord
            {
                CylinderName = _cylinder.Name,
                EventType = "Command",
                Message = $"{_cylinder.Name} close command sent"
            });
            OnPropertyChanged(nameof(State));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
