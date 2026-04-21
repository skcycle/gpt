using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisViewModel : INotifyPropertyChanged
{
    private readonly Axis _axis;
    private readonly AxisControlService _axisControlService;

    public AxisViewModel(Axis axis, AxisControlService axisControlService)
    {
        _axis = axis;
        _axisControlService = axisControlService;
        ClearAlarmCommand = new RelayCommand(async () => await ClearAlarmAsync(), () => HasAlarm);
    }

    public string Name => _axis.Name;
    public int AxisNo => _axis.Id.Value;
    public double CurrentPosition => _axis.CurrentPosition;
    public double EncoderPosition => _axis.EncoderPosition;
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
    public double CommandPositionMm => PulseEquivalent <= 0 ? 0 : CurrentPosition / PulseEquivalent;
    public double EncoderPositionMm => PulseEquivalent <= 0 ? 0 : EncoderPosition / PulseEquivalent;
    public string SoftLimitDisplay => _axis.SoftLimit is null ? "N/A" : $"{_axis.SoftLimit.Minimum} ~ {_axis.SoftLimit.Maximum}";
    public ICommand ClearAlarmCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentPosition));
        OnPropertyChanged(nameof(EncoderPosition));
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
        OnPropertyChanged(nameof(CommandPositionMm));
        OnPropertyChanged(nameof(EncoderPositionMm));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(HomeMode));
        OnPropertyChanged(nameof(ServoBinding));
        (ClearAlarmCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task ClearAlarmAsync()
    {
        try
        {
            await _axisControlService.ResetAlarmAsync(_axis);
            Refresh();
        }
        catch (InvalidOperationException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "报警清除失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
