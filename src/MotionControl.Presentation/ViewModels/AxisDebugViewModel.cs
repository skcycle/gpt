using MotionControl.Application.DTOs;
using MotionControl.Application.Interfaces;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisDebugViewModel
{
    private readonly IMotionAppService _motionAppService;
    private readonly Machine _machine;

    public AxisDebugViewModel(IMotionAppService motionAppService, Machine machine)
    {
        _motionAppService = motionAppService;
        _machine = machine;

        EnableAxisCommand = new RelayCommand(async () => await EnableSelectedAxisAsync(), () => SelectedAxisNo > 0);
        HomeAxisCommand = new RelayCommand(async () => await HomeSelectedAxisAsync(), () => SelectedAxisNo > 0);
        MoveAxisCommand = new RelayCommand(async () => await MoveSelectedAxisAsync(), () => SelectedAxisNo > 0);
        StopAxisCommand = new RelayCommand(async () => await StopSelectedAxisAsync(), () => SelectedAxisNo > 0);
        JogPositiveCommand = new RelayCommand(async () => await JogSelectedAxisAsync(true), () => SelectedAxisNo > 0);
        JogNegativeCommand = new RelayCommand(async () => await JogSelectedAxisAsync(false), () => SelectedAxisNo > 0);
    }

    public int SelectedAxisNo { get; set; }
    public double TargetPosition { get; set; }
    public double Velocity { get; set; } = 100;
    public double Acceleration { get; set; } = 100;
    public double Deceleration { get; set; } = 100;
    public string SelectedAxisHomeMode => SelectedAxis?.HomeMode.ToString() ?? "N/A";
    public string SelectedAxisServoBinding => SelectedAxis?.ServoBinding ?? "N/A";
    public string SelectedAxisSoftLimit => SelectedAxis?.SoftLimit is null ? "N/A" : $"{SelectedAxis.SoftLimit.Negative} ~ {SelectedAxis.SoftLimit.Positive}";
    public RelayCommand EnableAxisCommand { get; }
    public RelayCommand HomeAxisCommand { get; }
    public RelayCommand MoveAxisCommand { get; }
    public RelayCommand StopAxisCommand { get; }
    public RelayCommand JogPositiveCommand { get; }
    public RelayCommand JogNegativeCommand { get; }

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
}
