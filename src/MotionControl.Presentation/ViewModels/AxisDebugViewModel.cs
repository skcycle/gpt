using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Application.DTOs;
using MotionControl.Application.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisDebugViewModel : INotifyPropertyChanged
{
    private readonly IMotionAppService _motionAppService;
    private readonly Machine _machine;
    private readonly HomePlanRuntimeState _homePlanRuntimeState;
    private int _selectedAxisNo;
    private double _targetPosition;
    private double _velocity = 100;
    private double _acceleration = 100;
    private double _deceleration = 100;

    public AxisDebugViewModel(IMotionAppService motionAppService, Machine machine, HomePlanRuntimeState homePlanRuntimeState)
    {
        _motionAppService = motionAppService;
        _machine = machine;
        _homePlanRuntimeState = homePlanRuntimeState;

        EnableAxisCommand = new RelayCommand(async () => await EnableSelectedAxisAsync(), () => SelectedAxisNo > 0);
        HomeAxisCommand = new RelayCommand(async () => await HomeSelectedAxisAsync(), () => SelectedAxisNo > 0);
        MoveAxisCommand = new RelayCommand(async () => await MoveSelectedAxisAsync(), () => SelectedAxisNo > 0);
        StopAxisCommand = new RelayCommand(async () => await StopSelectedAxisAsync(), () => SelectedAxisNo > 0);
        JogPositiveCommand = new RelayCommand(async () => await JogSelectedAxisAsync(true), () => SelectedAxisNo > 0);
        JogNegativeCommand = new RelayCommand(async () => await JogSelectedAxisAsync(false), () => SelectedAxisNo > 0);
    }

    public int SelectedAxisNo
    {
        get => _selectedAxisNo;
        set
        {
            if (_selectedAxisNo == value)
            {
                return;
            }

            _selectedAxisNo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedAxisHomeMode));
            OnPropertyChanged(nameof(SelectedAxisServoBinding));
            OnPropertyChanged(nameof(SelectedAxisSoftLimit));
            OnPropertyChanged(nameof(SelectedHomePlanTitle));
            OnPropertyChanged(nameof(SelectedHomePlanSteps));
        }
    }

    public double TargetPosition
    {
        get => _targetPosition;
        set
        {
            _targetPosition = value;
            OnPropertyChanged();
        }
    }

    public double Velocity
    {
        get => _velocity;
        set
        {
            _velocity = value;
            OnPropertyChanged();
        }
    }

    public double Acceleration
    {
        get => _acceleration;
        set
        {
            _acceleration = value;
            OnPropertyChanged();
        }
    }

    public double Deceleration
    {
        get => _deceleration;
        set
        {
            _deceleration = value;
            OnPropertyChanged();
        }
    }

    public string SelectedAxisHomeMode => SelectedAxis?.HomeMode.ToString() ?? "N/A";
    public string SelectedAxisServoBinding => SelectedAxis?.ServoBinding ?? "N/A";
    public string SelectedAxisSoftLimit => SelectedAxis?.SoftLimit is null ? "N/A" : $"{SelectedAxis.SoftLimit.Negative} ~ {SelectedAxis.SoftLimit.Positive}";
    public string SelectedHomePlanTitle => _homePlanRuntimeState.CurrentPlan?.Title ?? "No plan generated";
    public IReadOnlyList<string> SelectedHomePlanSteps => _homePlanRuntimeState.CurrentPlan?.Steps ?? Array.Empty<string>();
    public RelayCommand EnableAxisCommand { get; }
    public RelayCommand HomeAxisCommand { get; }
    public RelayCommand MoveAxisCommand { get; }
    public RelayCommand StopAxisCommand { get; }
    public RelayCommand JogPositiveCommand { get; }
    public RelayCommand JogNegativeCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private Axis? SelectedAxis => _machine.Axes.FirstOrDefault(axis => axis.ControllerAxisNo == SelectedAxisNo);

    public async Task EnableSelectedAxisAsync()
    {
        await _motionAppService.EnableAxisAsync(new AxisCommandDto(SelectedAxisNo));
    }

    public async Task HomeSelectedAxisAsync()
    {
        await _motionAppService.HomeAxisAsync(new AxisCommandDto(SelectedAxisNo));
    }

    public async Task MoveSelectedAxisAsync()
    {
        await _motionAppService.MoveAbsoluteAsync(new MoveAxisCommandDto(SelectedAxisNo, TargetPosition, Velocity, Acceleration, Deceleration));
    }

    public async Task StopSelectedAxisAsync()
    {
        await _motionAppService.StopAxisAsync(new AxisCommandDto(SelectedAxisNo));
    }

    public async Task JogSelectedAxisAsync(bool positiveDirection)
    {
        await _motionAppService.JogAxisAsync(new JogAxisCommandDto(SelectedAxisNo, Velocity, positiveDirection));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
