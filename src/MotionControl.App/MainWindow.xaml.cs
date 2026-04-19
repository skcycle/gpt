using System.Windows;
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
}
