using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App;

public partial class MainWindow : Window
{
    private const int JogHoldThresholdMs = 300;

    private readonly MainWindowViewModel? _viewModel;
    private bool _initialized;
    private bool _jogHolding;
    private bool _jogContinuousRunning;
    private bool _jogPositiveDirection;
    private DispatcherTimer? _jogHoldTimer;

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

    private void JogPositiveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        ((UIElement)sender).CaptureMouse();

        _jogHolding = true;
        _jogContinuousRunning = false;
        _jogPositiveDirection = true;

        _jogHoldTimer?.Stop();
        _jogHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(JogHoldThresholdMs) };
        _jogHoldTimer.Tick += OnJogHoldTimerTick;
        _jogHoldTimer.Start();
    }

    private void JogNegativeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        ((UIElement)sender).CaptureMouse();

        _jogHolding = true;
        _jogContinuousRunning = false;
        _jogPositiveDirection = false;

        _jogHoldTimer?.Stop();
        _jogHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(JogHoldThresholdMs) };
        _jogHoldTimer.Tick += OnJogHoldTimerTick;
        _jogHoldTimer.Start();
    }

    private void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        ((UIElement)sender).ReleaseMouseCapture();

        if (_jogHoldTimer != null)
        {
            _jogHoldTimer.Stop();
            _jogHoldTimer.Tick -= OnJogHoldTimerTick;
            _jogHoldTimer = null;
        }

        _jogHolding = false;

        if (_jogContinuousRunning)
        {
            _jogContinuousRunning = false;
            _ = _viewModel.AxisDebug.StopJogAsync();
        }
        else
        {
            _ = _viewModel.AxisDebug.StepMoveAsync(_jogPositiveDirection);
        }
    }

    private void JogButton_MouseLeave(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            _jogHolding = false;

            if (_jogHoldTimer != null)
            {
                _jogHoldTimer.Stop();
                _jogHoldTimer.Tick -= OnJogHoldTimerTick;
                _jogHoldTimer = null;
            }
        }
    }

    private void OnJogHoldTimerTick(object? sender, EventArgs e)
    {
        if (_jogHoldTimer != null)
        {
            _jogHoldTimer.Stop();
            _jogHoldTimer.Tick -= OnJogHoldTimerTick;
            _jogHoldTimer = null;
        }

        if (!_jogHolding || _viewModel is null)
        {
            return;
        }

        _jogContinuousRunning = true;
        _ = _viewModel.AxisDebug.StartJogAsync(_jogPositiveDirection);
    }

    private void AxisMonitorDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = FindVisualChild<ScrollViewer>(this);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }

    private void AxisParametersScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }

    private void SectionDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        var dataGridScrollViewer = FindVisualChild<ScrollViewer>(dataGrid);
        if (dataGridScrollViewer != null)
        {
            var scrollingDown = e.Delta < 0;
            var scrollingUp = e.Delta > 0;
            var canScrollDown = dataGridScrollViewer.VerticalOffset < dataGridScrollViewer.ScrollableHeight;
            var canScrollUp = dataGridScrollViewer.VerticalOffset > 0;

            if ((scrollingDown && canScrollDown) || (scrollingUp && canScrollUp))
            {
                dataGridScrollViewer.ScrollToVerticalOffset(dataGridScrollViewer.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
                return;
            }
        }

        var parentScrollViewer = FindVisualChild<ScrollViewer>(this);
        if (parentScrollViewer != null)
        {
            parentScrollViewer.ScrollToVerticalOffset(parentScrollViewer.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e) => NavigateTo(0, MainWindowViewModel.NavigationPage.Dashboard);
    private void NavEtherCat_Click(object sender, RoutedEventArgs e) => NavigateTo(1, MainWindowViewModel.NavigationPage.EtherCat);
    private void NavAxis_Click(object sender, RoutedEventArgs e) => NavigateTo(2, MainWindowViewModel.NavigationPage.Axis);
    private void NavIo_Click(object sender, RoutedEventArgs e) => NavigateTo(3, MainWindowViewModel.NavigationPage.Io);
    private void NavCylinder_Click(object sender, RoutedEventArgs e) => NavigateTo(4, MainWindowViewModel.NavigationPage.Cylinder);
    private void NavMagazine_Click(object sender, RoutedEventArgs e) => NavigateTo(5, MainWindowViewModel.NavigationPage.Magazine);
    private void NavWorkHead_Click(object sender, RoutedEventArgs e) => NavigateTo(6, MainWindowViewModel.NavigationPage.WorkHead);
    private void NavPositionSetup_Click(object sender, RoutedEventArgs e) => NavigateTo(7, MainWindowViewModel.NavigationPage.PositionSetup);
    private void NavAlarm_Click(object sender, RoutedEventArgs e) => NavigateTo(8, MainWindowViewModel.NavigationPage.Alarm);

    private void NavigateTo(int tabIndex, MainWindowViewModel.NavigationPage page)
    {
        if (FindName("MainTabControl") is TabControl tabControl)
        {
            tabControl.SelectedIndex = tabIndex;
        }

        SetNavigationSelection(tabIndex);

        if (_viewModel is not null)
        {
            _viewModel.SelectedPage = page;
        }
    }

    private void SetNavigationSelection(int tabIndex)
    {
        if (FindName("NavDashboardButton") is System.Windows.Controls.Primitives.ToggleButton dashboardButton)
        {
            dashboardButton.IsChecked = tabIndex == 0;
        }
        if (FindName("NavEtherCatButton") is System.Windows.Controls.Primitives.ToggleButton etherCatButton)
        {
            etherCatButton.IsChecked = tabIndex == 1;
        }
        if (FindName("NavAxisButton") is System.Windows.Controls.Primitives.ToggleButton axisButton)
        {
            axisButton.IsChecked = tabIndex == 2;
        }
        if (FindName("NavIoButton") is System.Windows.Controls.Primitives.ToggleButton ioButton)
        {
            ioButton.IsChecked = tabIndex == 3;
        }
        if (FindName("NavCylinderButton") is System.Windows.Controls.Primitives.ToggleButton cylinderButton)
        {
            cylinderButton.IsChecked = tabIndex == 4;
        }
        if (FindName("NavMagazineButton") is System.Windows.Controls.Primitives.ToggleButton magazineButton)
        {
            magazineButton.IsChecked = tabIndex == 5;
        }
        if (FindName("NavWorkHeadButton") is System.Windows.Controls.Primitives.ToggleButton workHeadButton)
        {
            workHeadButton.IsChecked = tabIndex == 6;
        }
        if (FindName("NavPositionSetupButton") is System.Windows.Controls.Primitives.ToggleButton positionSetupButton)
        {
            positionSetupButton.IsChecked = tabIndex == 7;
        }
        if (FindName("NavAlarmButton") is System.Windows.Controls.Primitives.ToggleButton alarmButton)
        {
            alarmButton.IsChecked = tabIndex == 8;
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
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
