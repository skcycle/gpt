using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class IoPointViewModel : INotifyPropertyChanged
{
    private readonly IoPoint _ioPoint;
    private readonly IoControlService _ioControlService;
    private readonly Func<bool> _canToggleOutput;
    private bool _value;

    public IoPointViewModel(IoPoint ioPoint, IoControlService ioControlService, Func<bool> canToggleOutput)
    {
        _ioPoint = ioPoint;
        _ioControlService = ioControlService;
        _canToggleOutput = canToggleOutput;
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
            () => IsOutput && _canToggleOutput());
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

    public Brush StatusBrush => _value
        ? new SolidColorBrush(Color.FromRgb(0, 200, 0))   // 绿灯 - ON
        : new SolidColorBrush(Color.FromRgb(100, 100, 100)); // 灰灯 - OFF

    public ICommand SetCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshCommandState()
    {
        (SetCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public void Refresh()
    {
        var newValue = _ioPoint.Value;
        _value = newValue;
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(StatusBrush));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Address));
        OnPropertyChanged(nameof(Description));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
