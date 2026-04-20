using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class IoPointViewModel : INotifyPropertyChanged
{
    private readonly IoPoint _ioPoint;
    private readonly IoControlService _ioControlService;
    private bool _value;

    public IoPointViewModel(IoPoint ioPoint, IoControlService ioControlService)
    {
        _ioPoint = ioPoint;
        _ioControlService = ioControlService;
        _value = ioPoint.Value;
        SetCommand = new RelayCommand(
            async () =>
            {
                var nextValue = !_ioPoint.Value;
                var result = await _ioControlService.SetOutputAsync(Address, nextValue);
                if (result.Success)
                {
                    _ioPoint.Update(nextValue);
                    _value = nextValue;
                    OnPropertyChanged(nameof(Value));
                }
            },
            () => IsOutput);
    }

    public string Name
    {
        get => _ioPoint.Name;
        set
        {
            if (_ioPoint.Name == value) return;
            _ioPoint.UpdateMetadata(value, _ioPoint.Address, _ioPoint.Description);
            OnPropertyChanged();
        }
    }

    public int Address
    {
        get => _ioPoint.Address;
        set
        {
            if (_ioPoint.Address == value) return;
            _ioPoint.UpdateMetadata(_ioPoint.Name, value, _ioPoint.Description);
            OnPropertyChanged();
        }
    }

    public bool IsOutput => _ioPoint.IsOutput;
    public string Direction => _ioPoint.IsOutput ? "DO" : "DI";
    public string Description
    {
        get => _ioPoint.Description;
        set
        {
            if (_ioPoint.Description == value) return;
            _ioPoint.UpdateMetadata(_ioPoint.Name, _ioPoint.Address, value);
            OnPropertyChanged();
        }
    }

    public bool Value => _value;

    public ICommand SetCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        var newValue = _ioPoint.Value;
        _value = newValue;
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Address));
        OnPropertyChanged(nameof(Description));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
