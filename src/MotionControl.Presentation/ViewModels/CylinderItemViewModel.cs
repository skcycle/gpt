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
        ExtendCommand = new RelayCommand(async () => await SetOutputsAsync(true), () => _canControl());
        RetractCommand = new RelayCommand(async () => await SetOutputsAsync(false), () => _canControl());
        StopCommand = new RelayCommand(async () => await StopAsync(), () => _canControl());
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
    public ICommand ExtendCommand { get; }
    public ICommand RetractCommand { get; }
    public ICommand StopCommand { get; }

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
        (ExtendCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RetractCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task SetOutputsAsync(bool extend)
    {
        var first = await _ioControlService.SetOutputAsync(extend ? ExtendOutputAddress : RetractOutputAddress, true);
        var second = await _ioControlService.SetOutputAsync(extend ? RetractOutputAddress : ExtendOutputAddress, false);
        if (first.Success && second.Success)
        {
            if (extend)
            {
                _cylinder.StartExtendCommand();
            }
            else
            {
                _cylinder.StartRetractCommand();
            }

            _cylinderEventRuntimeState.Add(new CylinderEventRecord
            {
                CylinderName = _cylinder.Name,
                EventType = extend ? "Command" : "Command",
                Message = extend ? $"{_cylinder.Name} extend command sent" : $"{_cylinder.Name} retract command sent"
            });
            OnPropertyChanged(nameof(State));
        }
    }

    private async Task StopAsync()
    {
        await _ioControlService.SetOutputAsync(ExtendOutputAddress, false);
        await _ioControlService.SetOutputAsync(RetractOutputAddress, false);
        _cylinder.ClearPendingCommand();
        _cylinderEventRuntimeState.Add(new CylinderEventRecord
        {
            CylinderName = _cylinder.Name,
            EventType = "Command",
            Message = $"{_cylinder.Name} stop command sent"
        });
        OnPropertyChanged(nameof(State));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
