using System.Windows;
using System.Windows.Input;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel? _viewModel;
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized || _viewModel is null)
        {
            return;
        }

        _initialized = true;
        await _viewModel.InitializeAsync();
    }

    private async void JogPositiveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        await _viewModel.AxisDebug.StartJogAsync(true);
    }

    private async void JogNegativeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        await _viewModel.AxisDebug.StartJogAsync(false);
    }

    private async void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        await _viewModel.AxisDebug.StopJogAsync();
    }

    private async void JogButton_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_viewModel is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        await _viewModel.AxisDebug.StopJogAsync();
    }
}
