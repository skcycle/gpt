using System.Windows;
using MotionControl.Control.Services;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly PollingHostedService _pollingHostedService;

    public MainWindow(MainWindowViewModel viewModel, PollingHostedService pollingHostedService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _pollingHostedService = pollingHostedService;
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        _pollingHostedService.Start(TimeSpan.FromMilliseconds(200));
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        await _pollingHostedService.StopAsync();
    }
}
