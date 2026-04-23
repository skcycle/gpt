using System.Threading;
using System.Windows;
using System.Windows.Input;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App;

public partial class MainWindow : Window
{
    private const int JogHoldThresholdMs = 250;

    private readonly MainWindowViewModel? _viewModel;
    private bool _initialized;
    private CancellationTokenSource? _jogPressCts;
    private bool _isJogRunning;
    private bool? _pendingJogDirection;

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
        await BeginJogPressAsync(true);
    }

    private async void JogNegativeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        await BeginJogPressAsync(false);
    }

    private async void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        await EndJogPressAsync();
    }

    private async void JogButton_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_viewModel is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        await EndJogPressAsync();
    }

    private async Task BeginJogPressAsync(bool positiveDirection)
    {
        if (_viewModel is null)
        {
            return;
        }

        _jogPressCts?.Cancel();
        _jogPressCts?.Dispose();
        _jogPressCts = new CancellationTokenSource();
        _pendingJogDirection = positiveDirection;
        _isJogRunning = false;

        try
        {
            await Task.Delay(JogHoldThresholdMs, _jogPressCts.Token);
            if (_pendingJogDirection.HasValue)
            {
                _isJogRunning = true;
                await _viewModel.AxisDebug.StartJogAsync(_pendingJogDirection.Value);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task EndJogPressAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        _jogPressCts?.Cancel();
        _jogPressCts?.Dispose();
        _jogPressCts = null;

        var direction = _pendingJogDirection;
        _pendingJogDirection = null;

        if (_isJogRunning)
        {
            _isJogRunning = false;
            await _viewModel.AxisDebug.StopJogAsync();
            return;
        }

        if (direction.HasValue)
        {
            await _viewModel.AxisDebug.StepMoveAsync(direction.Value);
        }
    }

    private void AxisMonitorDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Forward mouse wheel to the parent ScrollViewer so the outer panel scrolls
        var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(this);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }

    private void AxisParametersScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is System.Windows.Controls.ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }


    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }

            var grandChild = FindVisualChild<T>(child);
            if (grandChild != null)
            {
                return grandChild;
            }
        }

        return null;
    }
}
