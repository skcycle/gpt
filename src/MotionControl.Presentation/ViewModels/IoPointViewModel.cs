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
            async () => await _ioControlService.SetOutputAsync(Address, !_value),
            () => IsOutput);
    }

    public string Name => _ioPoint.Name;
    public int Address => _ioPoint.Address;
    public bool IsOutput => _ioPoint.IsOutput;
    public string Direction => _ioPoint.IsOutput ? "DO" : "DI";
    public bool Value => _value;

    public ICommand SetCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        var newValue = _ioPoint.Value;
        _value = newValue;
        OnPropertyChanged(nameof(Value));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
