using MotionControl.Application.DTOs;
using MotionControl.Application.Interfaces;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisDebugViewModel
{
    private readonly IMotionAppService _motionAppService;

    public AxisDebugViewModel(IMotionAppService motionAppService)
    {
        _motionAppService = motionAppService;

        EnableAxisCommand = new RelayCommand(async () => await EnableSelectedAxisAsync(), () => SelectedAxisNo > 0);
        HomeAxisCommand = new RelayCommand(async () => await HomeSelectedAxisAsync(), () => SelectedAxisNo > 0);
    }

    public int SelectedAxisNo { get; set; }
    public RelayCommand EnableAxisCommand { get; }
    public RelayCommand HomeAxisCommand { get; }

    public async Task EnableSelectedAxisAsync()
    {
        await _motionAppService.EnableAxisAsync(new AxisCommandDto(SelectedAxisNo));
    }

    public async Task HomeSelectedAxisAsync()
    {
        await _motionAppService.HomeAxisAsync(new AxisCommandDto(SelectedAxisNo));
    }
}
