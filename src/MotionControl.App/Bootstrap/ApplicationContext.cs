using MotionControl.Control.Services;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App.Bootstrap;

public sealed class ApplicationContext(
    MainWindowViewModel mainWindowViewModel,
    PollingHostedService pollingHostedService)
{
    public MainWindowViewModel MainWindowViewModel { get; } = mainWindowViewModel;
    public PollingHostedService PollingHostedService { get; } = pollingHostedService;
}
