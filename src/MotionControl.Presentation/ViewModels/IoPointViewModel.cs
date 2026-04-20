using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class IoPointViewModel : INotifyPropertyChanged
{
    private readonly IoPoint _ioPoint;

    public IoPointViewModel(IoPoint ioPoint)
    {
        _ioPoint = ioPoint;
    }

    public string Name => _ioPoint.Name;
    public int Address => _ioPoint.Address;
    public bool IsOutput => _ioPoint.IsOutput;
    public string Direction => _ioPoint.IsOutput ? "DO" : "DI";
    public bool Value => _ioPoint.Value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        OnPropertyChanged(nameof(Value));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
