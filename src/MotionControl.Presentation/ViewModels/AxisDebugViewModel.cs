using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Application.DTOs;
using MotionControl.Application.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Device.Abstractions.Results;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisDebugViewModel : INotifyPropertyChanged
{
    private readonly IMotionAppService _motionAppService;
    private readonly Machine _machine;
    private readonly HomePlanRuntimeState _homePlanRuntimeState;
    private readonly Func<bool> _canControlAxis;
    private int _selectedAxisNo;
    private double _targetPosition;
    private double _velocity = 100;
    private double _acceleration = 100;
    private double _deceleration = 100;
    private double _jogStepDistance = 1;

    public AxisDebugViewModel(IMotionAppService motionAppService, Machine machine, HomePlanRuntimeState homePlanRuntimeState, Func<bool> canControlAxis)
    {
        _motionAppService = motionAppService;
        _machine = machine;
        _homePlanRuntimeState = homePlanRuntimeState;
        _canControlAxis = canControlAxis;

        EnableAxisCommand = new RelayCommand(async () => await EnableSelectedAxisAsync(), CanExecuteAxisCommand);
        DisableAxisCommand = new RelayCommand(async () => await DisableSelectedAxisAsync(), CanExecuteAxisCommand);
        HomeAxisCommand = new RelayCommand(async () => await HomeSelectedAxisAsync(), CanExecuteAxisCommand);
        MoveAxisCommand = new RelayCommand(async () => await MoveSelectedAxisAsync(), CanExecuteAxisCommand);
        StopAxisCommand = new RelayCommand(async () => await StopSelectedAxisAsync(), CanExecuteAxisCommand);
        JogPositiveCommand = new RelayCommand(async () => await StartJogAsync(true), CanExecuteAxisCommand);
        JogNegativeCommand = new RelayCommand(async () => await StartJogAsync(false), CanExecuteAxisCommand);
    }

    public event Action<int>? SelectedAxisChanged;

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
            EnableAxisCommand.RaiseCanExecuteChanged();
            HomeAxisCommand.RaiseCanExecuteChanged();
            MoveAxisCommand.RaiseCanExecuteChanged();
            StopAxisCommand.RaiseCanExecuteChanged();
            JogPositiveCommand.RaiseCanExecuteChanged();
            JogNegativeCommand.RaiseCanExecuteChanged();
            SelectedAxisChanged?.Invoke(_selectedAxisNo);
        }
    }

    public void RefreshCommandStates()
    {
        EnableAxisCommand.RaiseCanExecuteChanged();
        HomeAxisCommand.RaiseCanExecuteChanged();
        MoveAxisCommand.RaiseCanExecuteChanged();
        StopAxisCommand.RaiseCanExecuteChanged();
        JogPositiveCommand.RaiseCanExecuteChanged();
        JogNegativeCommand.RaiseCanExecuteChanged();
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

    public double JogStepDistance
    {
        get => _jogStepDistance;
        set
        {
            if (_jogStepDistance == value)
            {
                return;
            }

            _jogStepDistance = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<double> JogStepDistances { get; } = new[] { 0.1d, 1d, 10d };

    public string SelectedAxisHomeMode => SelectedAxis?.HomeMode.ToString() ?? "N/A";
    public string SelectedAxisServoBinding => SelectedAxis?.ServoBinding ?? "N/A";
    public string SelectedAxisSoftLimit => SelectedAxis?.SoftLimit is null ? "N/A" : $"{SelectedAxis.SoftLimit.Minimum} ~ {SelectedAxis.SoftLimit.Maximum}";
    public string SelectedHomePlanTitle => _homePlanRuntimeState.CurrentPlan?.Title ?? "No plan generated";
    public IReadOnlyList<string> SelectedHomePlanSteps => _homePlanRuntimeState.CurrentPlan?.Steps ?? Array.Empty<string>();
    public RelayCommand EnableAxisCommand { get; }
    public RelayCommand DisableAxisCommand { get; }
    public RelayCommand HomeAxisCommand { get; }
    public RelayCommand MoveAxisCommand { get; }
    public RelayCommand StopAxisCommand { get; }
    public RelayCommand JogPositiveCommand { get; }
    public RelayCommand JogNegativeCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private Axis? SelectedAxis => _machine.Axes.FirstOrDefault(axis => axis.Id.Value == SelectedAxisNo);

    private bool CanExecuteAxisCommand() => SelectedAxis is not null && _canControlAxis();

    public async Task EnableSelectedAxisAsync()
    {
        var r = await _motionAppService.EnableAxisAsync(new AxisCommandDto(SelectedAxisNo));
        if (!r.Success) System.Diagnostics.Debug.WriteLine($"[Axis] 使能失败: {r.ErrorMessage}");
    }

    public async Task DisableSelectedAxisAsync()
    {
        var r = await _motionAppService.DisableAxisAsync(new AxisCommandDto(SelectedAxisNo));
        if (!r.Success) System.Diagnostics.Debug.WriteLine($"[Axis] 失能失败: {r.ErrorMessage}");
    }

    public async Task HomeSelectedAxisAsync()
    {
        var r = await _motionAppService.HomeAxisAsync(new AxisCommandDto(SelectedAxisNo));
        if (!r.Success) System.Diagnostics.Debug.WriteLine($"[Axis] 回零失败: {r.ErrorMessage}");
    }

    public async Task MoveSelectedAxisAsync()
    {
        var r = await _motionAppService.MoveAbsoluteAsync(new MoveAxisCommandDto(SelectedAxisNo, TargetPosition, Velocity, Acceleration, Deceleration));
        if (!r.Success) System.Diagnostics.Debug.WriteLine($"[Axis] 定位失败: {r.ErrorMessage}");
    }

    public async Task StopSelectedAxisAsync()
    {
        var r = await _motionAppService.StopAxisAsync(new AxisCommandDto(SelectedAxisNo));
        if (!r.Success) System.Diagnostics.Debug.WriteLine($"[Axis] 停止失败: {r.ErrorMessage}");
    }

    public async Task StartJogAsync(bool positiveDirection)
    {
        if (SelectedAxis is null)
        {
            return;
        }

        var r = await _motionAppService.JogAxisAsync(new JogAxisCommandDto(SelectedAxisNo, Velocity, positiveDirection));
        if (!r.Success) System.Diagnostics.Debug.WriteLine($"[Axis] Jog 失败: {r.ErrorMessage}");
    }

    public async Task StopJogAsync()
    {
        if (SelectedAxis is null)
        {
            return;
        }

        var r = await _motionAppService.StopAxisAsync(new AxisCommandDto(SelectedAxisNo));
        if (!r.Success) System.Diagnostics.Debug.WriteLine($"[Axis] 停止失败: {r.ErrorMessage}");
    }

    public async Task StepMoveAsync(bool positiveDirection)
    {
        if (SelectedAxis is null)
        {
            return;
        }

        var pulseEquivalent = SelectedAxis.PulseEquivalent > 0 ? SelectedAxis.PulseEquivalent : 1000;
        var currentPositionMm = SelectedAxis.CurrentPosition / pulseEquivalent;
        var step = positiveDirection ? JogStepDistance : -JogStepDistance;
        var targetPositionMm = currentPositionMm + step;
        await _motionAppService.MoveAbsoluteAsync(new MoveAxisCommandDto(SelectedAxisNo, targetPositionMm, Velocity, Acceleration, Deceleration));
        TargetPosition = targetPositionMm;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
