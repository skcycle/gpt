using MotionControl.Application.Interfaces;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class MainWindowViewModel
{
    private readonly ISystemAppService _systemAppService;

    public MainWindowViewModel(
        Machine machine,
        ISystemAppService systemAppService,
        IMotionAppService motionAppService)
    {
        _systemAppService = systemAppService;
        AxisMonitor = new AxisMonitorViewModel(machine);
        AxisDebug = new AxisDebugViewModel(motionAppService);
    }

    public AxisMonitorViewModel AxisMonitor { get; }
    public AxisDebugViewModel AxisDebug { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _systemAppService.InitializeAsync(cancellationToken);
        AxisMonitor.RefreshAll();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _systemAppService.RefreshAsync(cancellationToken);
        AxisMonitor.RefreshAll();
    }
}
