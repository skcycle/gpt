using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisViewModel : INotifyPropertyChanged
{
    private readonly Axis _axis;

    public AxisViewModel(Axis axis)
    {
        _axis = axis;
    }

    public string Name => _axis.Name;
    public int AxisNo => _axis.ControllerAxisNo;
    public double CurrentPosition => _axis.CurrentPosition;
    public double CurrentVelocity => _axis.CurrentVelocity;
    public bool HasAlarm => _axis.HasAlarm;
    public bool IsHomed => _axis.IsHomed;
    public string HomeMode => _axis.HomeMode.ToString();
    public string ServoBinding => _axis.ServoBinding;
    public string SoftLimitDisplay => _axis.SoftLimit is null ? "N/A" : $"{_axis.SoftLimit.Negative} ~ {_axis.SoftLimit.Positive}";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentPosition));
        OnPropertyChanged(nameof(CurrentVelocity));
        OnPropertyChanged(nameof(HasAlarm));
        OnPropertyChanged(nameof(IsHomed));
        OnPropertyChanged(nameof(HomeMode));
        OnPropertyChanged(nameof(ServoBinding));
        OnPropertyChanged(nameof(SoftLimitDisplay));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
