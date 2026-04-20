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
    public int AxisNo => _axis.Id.Value;
    public double CurrentPosition => _axis.CurrentPosition;
    public double CurrentVelocity => _axis.CurrentVelocity;
    public bool HasAlarm => _axis.HasAlarm;
    public bool IsHomed => _axis.IsHomed;
    public bool IsServoOn => _axis.ServoState == Domain.Enums.ServoState.On;
    public bool PositiveSoftLimitTriggered => _axis.PositiveSoftLimitTriggered;
    public bool NegativeSoftLimitTriggered => _axis.NegativeSoftLimitTriggered;
    public bool PositiveHardLimitTriggered => _axis.PositiveLimitTriggered;
    public bool NegativeHardLimitTriggered => _axis.NegativeLimitTriggered;
    public string HomeMode => _axis.HomeMode.ToString();
    public string ServoBinding => _axis.ServoBinding;
    public double WorkVelocity => _axis.WorkVelocity;
    public double SetupVelocity => _axis.SetupVelocity;
    public double PulseEquivalent => _axis.PulseEquivalent;
    public string SoftLimitDisplay => _axis.SoftLimit is null ? "N/A" : $"{_axis.SoftLimit.Minimum} ~ {_axis.SoftLimit.Maximum}";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentPosition));
        OnPropertyChanged(nameof(CurrentVelocity));
        OnPropertyChanged(nameof(HasAlarm));
        OnPropertyChanged(nameof(IsHomed));
        OnPropertyChanged(nameof(IsServoOn));
        OnPropertyChanged(nameof(PositiveSoftLimitTriggered));
        OnPropertyChanged(nameof(NegativeSoftLimitTriggered));
        OnPropertyChanged(nameof(PositiveHardLimitTriggered));
        OnPropertyChanged(nameof(NegativeHardLimitTriggered));
        OnPropertyChanged(nameof(WorkVelocity));
        OnPropertyChanged(nameof(SetupVelocity));
        OnPropertyChanged(nameof(PulseEquivalent));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
