using System.Windows;
using MotionControl.App.Bootstrap;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly Bootstrap.ApplicationContext _applicationContext;

    public MainWindow()
    {
        InitializeComponent();
        _applicationContext = ServiceRegistration.BuildApplicationContext();
        _viewModel = _applicationContext.MainWindowViewModel;
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        _applicationContext.PollingHostedService.Start(TimeSpan.FromMilliseconds(200));
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        await _applicationContext.PollingHostedService.StopAsync();
    }
}
