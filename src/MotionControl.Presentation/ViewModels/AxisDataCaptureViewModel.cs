using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

/// <summary>
/// 轴运动数据采集与曲线绘制 ViewModel。
/// 以 1ms 频率采集 Speed、Cmd Position、Encoder Position，
/// 支持信号选择、采集时长配置、自动 Y 轴缩放。
/// 采用双 Y 轴：左轴 = Speed，右轴 = Position (Cmd/Enc 共用)。
/// </summary>
public sealed class AxisDataCaptureViewModel : INotifyPropertyChanged
{
    private readonly Machine _machine;
    private readonly Func<bool> _canControlAxis;
    private CancellationTokenSource? _captureCts;

    public sealed class CapturePoint
    {
        public double TimeMs { get; init; }
        public double Speed { get; init; }
        public double CmdPositionMm { get; init; }
        public double EncPositionMm { get; init; }
    }

    public AxisDataCaptureViewModel(Machine machine, Func<bool> canControlAxis)
    {
        _machine = machine;
        _canControlAxis = canControlAxis;

        StartCaptureCommand = new RelayCommand(async () => await StartCaptureAsync(), () => !IsCapturing && CanControl());
        StopCaptureCommand = new RelayCommand(StopCapture, () => IsCapturing);
        ClearCommand = new RelayCommand(Clear, () => DataPointCount > 0 && !IsCapturing);
        CaptureDurations = new[] { 500, 1000, 2000, 5000, 10000 };
    }

    // --- 信号选择 ---
    private bool _showSpeed = true;
    public bool ShowSpeed
    {
        get => _showSpeed;
        set { if (_showSpeed == value) return; _showSpeed = value; OnPropertyChanged(); UpdateChart(); }
    }

    private bool _showCmdPosition = true;
    public bool ShowCmdPosition
    {
        get => _showCmdPosition;
        set { if (_showCmdPosition == value) return; _showCmdPosition = value; OnPropertyChanged(); UpdateChart(); }
    }

    private bool _showEncPosition = true;
    public bool ShowEncPosition
    {
        get => _showEncPosition;
        set { if (_showEncPosition == value) return; _showEncPosition = value; OnPropertyChanged(); UpdateChart(); }
    }

    // --- 采集参数 ---
    private int _captureDurationMs = 1000;
    public int CaptureDurationMs
    {
        get => _captureDurationMs;
        set { if (_captureDurationMs == value) return; _captureDurationMs = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<int> CaptureDurations { get; }

    private bool _isCapturing;
    public bool IsCapturing
    {
        get => _isCapturing;
        private set
        {
            if (_isCapturing == value) return;
            _isCapturing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            StartCaptureCommand.RaiseCanExecuteChanged();
            StopCaptureCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
        }
    }

    private int _dataPointCount;
    public int DataPointCount
    {
        get => _dataPointCount;
        private set { if (_dataPointCount == value) return; _dataPointCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); ClearCommand.RaiseCanExecuteChanged(); }
    }

    public string StatusText => IsCapturing
        ? $"Capturing... {DataPointCount} pts"
        : DataPointCount > 0
            ? $"Idle — {DataPointCount} points ({_lastTimeMs:F0} ms)"
            : "Idle — no data";

    private double _lastTimeMs;

    // Internal list for fast capture; converted to chart data on update
    private readonly List<CapturePoint> _captureBuffer = new();

    // --- 图表数据 (绑定到 Polyline) ---
    private PointCollection? _speedLinePoints;
    public PointCollection? SpeedLinePoints
    {
        get => _speedLinePoints;
        private set { _speedLinePoints = value; OnPropertyChanged(); }
    }

    private PointCollection? _cmdLinePoints;
    public PointCollection? CmdLinePoints
    {
        get => _cmdLinePoints;
        private set { _cmdLinePoints = value; OnPropertyChanged(); }
    }

    private PointCollection? _encLinePoints;
    public PointCollection? EncLinePoints
    {
        get => _encLinePoints;
        private set { _encLinePoints = value; OnPropertyChanged(); }
    }

    // --- 画布尺寸 ---
    private double _canvasWidth = 600;
    private double _canvasHeight = 280;

    public void UpdateCanvasSize(double width, double height)
    {
        if (Math.Abs(_canvasWidth - width) < 0.5 && Math.Abs(_canvasHeight - height) < 0.5)
            return;
        _canvasWidth = width;
        _canvasHeight = height;
        UpdateChart();
    }

    // --- 轴范围 (双Y轴，供 code-behind 绘制标尺) ---
    private double _speedMax;
    public double SpeedMax
    {
        get => _speedMax;
        set { if (Math.Abs(_speedMax - value) < 0.01) return; _speedMax = value; OnPropertyChanged(); }
    }

    private double _positionMax;
    public double PositionMax
    {
        get => _positionMax;
        set { if (Math.Abs(_positionMax - value) < 0.01) return; _positionMax = value; OnPropertyChanged(); }
    }

    private double _timeMax;
    public double TimeMax
    {
        get => _timeMax;
        set { if (Math.Abs(_timeMax - value) < 0.1) return; _timeMax = value; OnPropertyChanged(); }
    }

    // --- 边距常量 ---
    private const double MarginLeft = 50;
    private const double MarginRight = 20;
    private const double MarginTop = 20;
    private const double MarginBottom = 30;

    public RelayCommand StartCaptureCommand { get; }
    public RelayCommand StopCaptureCommand { get; }
    public RelayCommand ClearCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool CanControl() => _canControlAxis();

    private int _selectedAxisNo;

    public void SetSelectedAxisNo(int axisNo)
    {
        if (_selectedAxisNo == axisNo) return;
        _selectedAxisNo = axisNo;
        Clear();
    }

    private async Task StartCaptureAsync()
    {
        _captureBuffer.Clear();
        DataPointCount = 0;
        _lastTimeMs = 0;
        IsCapturing = true;

        _captureCts = new CancellationTokenSource();
        var token = _captureCts.Token;
        var stopwatch = Stopwatch.StartNew();
        var durationMs = CaptureDurationMs;
        var axisNo = _selectedAxisNo;

        try
        {
            while (stopwatch.ElapsedMilliseconds < durationMs && !token.IsCancellationRequested)
            {
                var axis = _machine.Axes.FirstOrDefault(a => a.Id.Value == axisNo);
                if (axis != null)
                {
                    var pulseEq = axis.PulseEquivalent > 0 ? axis.PulseEquivalent : 1000;
                    _captureBuffer.Add(new CapturePoint
                    {
                        TimeMs = stopwatch.ElapsedMilliseconds,
                        Speed = axis.CurrentVelocity,
                        CmdPositionMm = axis.CurrentPosition / pulseEq,
                        EncPositionMm = axis.EncoderPosition / pulseEq
                    });
                }

                DataPointCount = _captureBuffer.Count;

                // 1ms 间隔（尽力而为，实际精度受系统定时器限制）
                var elapsed = stopwatch.Elapsed;
                var nextTick = TimeSpan.FromMilliseconds(_captureBuffer.Count);
                var delay = nextTick - elapsed;
                if (delay > TimeSpan.Zero)
                {
                    try { await Task.Delay(delay, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            stopwatch.Stop();
            _lastTimeMs = _captureBuffer.Count > 0 ? _captureBuffer[^1].TimeMs : 0;
            IsCapturing = false;
            OnPropertyChanged(nameof(StatusText));
            // UpdateChart must run on UI thread (creates PointCollection / Freezable)
            UpdateChart();
        }
    }

    private void StopCapture()
    {
        _captureCts?.Cancel();
    }

    private void Clear()
    {
        _captureBuffer.Clear();
        DataPointCount = 0;
        _lastTimeMs = 0;
        SpeedLinePoints = null;
        CmdLinePoints = null;
        EncLinePoints = null;
        SpeedMax = 0;
        PositionMax = 0;
        TimeMax = 0;
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>
    /// 将捕捉缓冲区的数据点映射到 Canvas 坐标系并构建 Polyline。
    /// 双 Y 轴方案：左轴 = Speed，右轴 = Position (Cmd/Enc 共用)。
    /// 坐标轴随数据最大值自动缩放（含 15% 上边距）。
    /// </summary>
    public void UpdateChart()
    {
        if (_captureBuffer.Count == 0)
        {
            SpeedLinePoints = null;
            CmdLinePoints = null;
            EncLinePoints = null;
            SpeedMax = 0;
            PositionMax = 0;
            TimeMax = 0;
            return;
        }

        var canvasW = _canvasWidth - MarginLeft - MarginRight;
        var canvasH = _canvasHeight - MarginTop - MarginBottom;
        if (canvasW <= 0 || canvasH <= 0) return;

        var timeRange = _captureBuffer[^1].TimeMs;
        if (timeRange <= 0) timeRange = 1;
        TimeMax = timeRange;

        // 自动计算 Y 轴范围
        var speedRange = 1.0;
        var posRange = 1.0;
        if (ShowSpeed)
        {
            var maxV = _captureBuffer.Max(p => Math.Abs(p.Speed));
            speedRange = maxV > 0 ? maxV * 1.15 : 1.0;
        }
        if (ShowCmdPosition || ShowEncPosition)
        {
            var pmax = 0.0;
            if (ShowCmdPosition) pmax = _captureBuffer.Max(p => Math.Abs(p.CmdPositionMm));
            if (ShowEncPosition) pmax = Math.Max(pmax, _captureBuffer.Max(p => Math.Abs(p.EncPositionMm)));
            posRange = pmax > 0 ? pmax * 1.15 : 1.0;
        }

        SpeedMax = Math.Ceiling(speedRange);
        PositionMax = Math.Ceiling(posRange);

        double MapX(double t) => MarginLeft + t / timeRange * canvasW;
        double MapSpeedY(double v) => MarginTop + canvasH - v / speedRange * canvasH;
        double MapPosY(double p) => MarginTop + canvasH - p / posRange * canvasH;

        SpeedLinePoints = ShowSpeed
            ? new PointCollection(_captureBuffer.Select(p => new Point(MapX(p.TimeMs), MapSpeedY(p.Speed))))
            : null;

        CmdLinePoints = ShowCmdPosition
            ? new PointCollection(_captureBuffer.Select(p => new Point(MapX(p.TimeMs), MapPosY(p.CmdPositionMm))))
            : null;

        EncLinePoints = ShowEncPosition
            ? new PointCollection(_captureBuffer.Select(p => new Point(MapX(p.TimeMs), MapPosY(p.EncPositionMm))))
            : null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
