using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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

        // Redraw chart grid when axis data capture properties change
        _viewModel.AxisDataCapture.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName is nameof(AxisDataCaptureViewModel.ShowSpeed)
                or nameof(AxisDataCaptureViewModel.ShowCmdPosition)
                or nameof(AxisDataCaptureViewModel.ShowEncPosition)
                or nameof(AxisDataCaptureViewModel.SpeedMax)
                or nameof(AxisDataCaptureViewModel.PositionMax)
                or nameof(AxisDataCaptureViewModel.TimeMax))
            {
                Dispatcher.Invoke(() =>
                {
                    if (FindName("MotionCurveCanvas") is Canvas canvas)
                        DrawChartGrid(canvas);
                });
            }
        };

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

    private void MotionCurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Canvas canvas && _viewModel?.AxisDataCapture != null)
        {
            _viewModel.AxisDataCapture.UpdateCanvasSize(canvas.ActualWidth, canvas.ActualHeight);
            DrawChartGrid(canvas);
        }
    }

    private void DrawChartGrid(Canvas canvas)
    {
        var vm = _viewModel?.AxisDataCapture;
        if (vm == null) return;

        // Remove old grid/label elements (keep data polylines)
        var toRemove = canvas.Children.OfType<UIElement>()
            .Where(c => c is not Polyline && c is FrameworkElement fe && fe.Tag?.ToString() == "chart-label")
            .ToList();
        foreach (var item in toRemove)
            canvas.Children.Remove(item);

        const double marginLeft = 50, marginRight = 20, marginTop = 20, marginBottom = 32;
        var width = canvas.ActualWidth;
        var height = canvas.ActualHeight;
        var plotWidth = width - marginLeft - marginRight;
        var plotHeight = height - marginTop - marginBottom;
        if (plotWidth <= 0 || plotHeight <= 0) return;

        // Horizontal grid lines (5 lines = 4 segments)
        var gridBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x2E, 0x3D));
        for (int i = 0; i <= 4; i++)
        {
            double y = marginTop + (plotHeight * i / 4.0);
            canvas.Children.Add(new Line
            {
                X1 = marginLeft, Y1 = y, X2 = width - marginRight, Y2 = y,
                Stroke = gridBrush, StrokeThickness = 0.5,
                Tag = "chart-label"
            });
        }

        // Vertical grid lines (6 lines = 5 segments)
        for (int i = 0; i <= 5; i++)
        {
            double x = marginLeft + (plotWidth * i / 5.0);
            canvas.Children.Add(new Line
            {
                X1 = x, Y1 = marginTop, X2 = x, Y2 = height - marginBottom,
                Stroke = gridBrush, StrokeThickness = 0.5,
                Tag = "chart-label"
            });
        }

        // Axes
        var axisBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x5A, 0x6C));
        canvas.Children.Add(new Line
        {
            X1 = marginLeft, Y1 = marginTop, X2 = marginLeft, Y2 = height - marginBottom,
            Stroke = axisBrush, StrokeThickness = 1,
            Tag = "chart-label"
        });
        canvas.Children.Add(new Line
        {
            X1 = marginLeft, Y1 = height - marginBottom, X2 = width - marginRight, Y2 = height - marginBottom,
            Stroke = axisBrush, StrokeThickness = 1,
            Tag = "chart-label"
        });

        var labelBrush = new SolidColorBrush(Color.FromRgb(0x7A, 0x8E, 0x9E));
        var labelFontSize = 9.0;

        // Speed labels (left Y axis)
        var speedMax = vm.SpeedMax > 0 ? vm.SpeedMax : 10;
        for (int i = 0; i <= 4; i++)
        {
            double val = speedMax * (4 - i) / 4.0;
            double y = marginTop + (plotHeight * i / 4.0);
            var tb = new TextBlock
            {
                Text = val.ToString("F0"),
                Foreground = labelBrush,
                FontSize = labelFontSize,
                Tag = "chart-label"
            };
            Canvas.SetLeft(tb, 3);
            Canvas.SetTop(tb, y - 7);
            canvas.Children.Add(tb);
        }

        // Position labels (right Y axis) — only if position signal visible
        if (vm.ShowCmdPosition || vm.ShowEncPosition)
        {
            var posMax = vm.PositionMax > 0 ? vm.PositionMax : 10;
            for (int i = 0; i <= 4; i++)
            {
                double val = posMax * (4 - i) / 4.0;
                double y = marginTop + (plotHeight * i / 4.0);
                var tb = new TextBlock
                {
                    Text = val.ToString("F1"),
                    Foreground = labelBrush,
                    FontSize = labelFontSize,
                    Tag = "chart-label"
                };
                Canvas.SetLeft(tb, width - marginRight + 4);
                Canvas.SetTop(tb, y - 7);
                canvas.Children.Add(tb);
            }
        }

        // Time labels (X axis)
        var timeMax = vm.TimeMax > 0 ? vm.TimeMax : 1000;
        for (int i = 0; i <= 5; i++)
        {
            double val = timeMax * i / 5.0;
            double x = marginLeft + (plotWidth * i / 5.0);
            var tb = new TextBlock
            {
                Text = val.ToString("F0"),
                Foreground = labelBrush,
                FontSize = labelFontSize,
                Tag = "chart-label"
            };
            Canvas.SetLeft(tb, x - 12);
            Canvas.SetTop(tb, height - marginBottom + 5);
            canvas.Children.Add(tb);
        }

        // X axis unit label
        var xUnitTb = new TextBlock
        {
            Text = "ms",
            Foreground = labelBrush,
            FontSize = labelFontSize,
            Tag = "chart-label"
        };
        Canvas.SetLeft(xUnitTb, width - marginRight - 16);
        Canvas.SetTop(xUnitTb, height - marginBottom + 5);
        canvas.Children.Add(xUnitTb);

        // Legend
        double legendX = marginLeft + 8;
        double legendY = marginTop + 4;
        var legendItems = new[]
        {
            (Text: "Speed", Color: Color.FromRgb(0xF0, 0xC0, 0x40), Show: vm.ShowSpeed),
            (Text: "Cmd Pos", Color: Color.FromRgb(0x4F, 0xC3, 0xF7), Show: vm.ShowCmdPosition),
            (Text: "Enc Pos", Color: Color.FromRgb(0x81, 0xC7, 0x84), Show: vm.ShowEncPosition),
        };
        foreach (var (text, color, show) in legendItems)
        {
            if (!show) continue;
            var dot = new Ellipse
            {
                Width = 7, Height = 7,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Color.FromRgb(0x44, 0x5A, 0x6C)),
                StrokeThickness = 0.5,
                Tag = "chart-label"
            };
            Canvas.SetLeft(dot, legendX);
            Canvas.SetTop(dot, legendY + 3);
            canvas.Children.Add(dot);
            var tb = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                FontSize = 11,
                Tag = "chart-label"
            };
            Canvas.SetLeft(tb, legendX + 11);
            Canvas.SetTop(tb, legendY);
            canvas.Children.Add(tb);
            legendX += 72;
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
