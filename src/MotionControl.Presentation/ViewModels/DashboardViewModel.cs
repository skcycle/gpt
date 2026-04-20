using MotionControl.Control.Services;
using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class DashboardViewModel
{
    private readonly Machine _machine;
    private readonly CommandFeedbackRuntimeState _commandFeedbackRuntimeState;
    private EtherCatControllerStatus? _controllerStatus;
    private string[] _lastRecentCommandFeedback = Array.Empty<string>();
    private string[] _lastActiveAlarmSummary = Array.Empty<string>();
    private EtherCatSlaveViewModel[] _lastEtherCatSlaves = Array.Empty<EtherCatSlaveViewModel>();

    public DashboardViewModel(Machine machine, CommandFeedbackRuntimeState commandFeedbackRuntimeState)
    {
        _machine = machine;
        _commandFeedbackRuntimeState = commandFeedbackRuntimeState;
    }

    public string SystemState => _machine.CurrentState.ToString();
    public int AxisCount => _machine.Axes.Count;
    public int AlarmCount => _machine.Alarms.Count(alarm => alarm.IsActive);
    public int ActiveInputCount => _machine.IoPoints.Count(io => !io.IsOutput && io.Value);
    public int ActiveOutputCount => _machine.IoPoints.Count(io => io.IsOutput && io.Value);
    public string EtherCatNetworkState => _controllerStatus?.NetworkState ?? "Unknown";
    public bool EtherCatConnected => _controllerStatus?.IsConnected ?? false;
    public int EtherCatOnlineSlaveCount => _controllerStatus?.OnlineSlaveCount ?? 0;
    public IReadOnlyList<EtherCatSlaveViewModel> EtherCatSlaves { get; private set; } = Array.Empty<EtherCatSlaveViewModel>();
    public IReadOnlyList<string> RecentCommandFeedback { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> ActiveAlarmSummary { get; private set; } = Array.Empty<string>();

    public void Refresh(EtherCatControllerStatus? controllerStatus = null)
    {
        _controllerStatus = controllerStatus ?? _controllerStatus;

        var latestSlaves = _controllerStatus?.Slaves.Select(slave => new EtherCatSlaveViewModel(slave)).ToArray()
            ?? Array.Empty<EtherCatSlaveViewModel>();
        if (!_lastEtherCatSlaves.SequenceEqual(latestSlaves))
        {
            EtherCatSlaves = latestSlaves;
            _lastEtherCatSlaves = latestSlaves;
        }

        var latestFeedback = _commandFeedbackRuntimeState.RecentFeedback
            .Reverse()
            .Take(8)
            .Select(item => $"[{item.Status}] {item.CommandName} Axis={item.AxisNo?.ToString() ?? "-"} {item.Message}")
            .ToArray();
        if (!_lastRecentCommandFeedback.SequenceEqual(latestFeedback))
        {
            RecentCommandFeedback = latestFeedback;
            _lastRecentCommandFeedback = latestFeedback;
        }

        var latestAlarmSummary = _machine.Alarms
            .Where(alarm => alarm.IsActive)
            .OrderByDescending(alarm => alarm.OccurredAt)
            .Take(5)
            .Select(alarm => $"[{alarm.Severity}] {alarm.Code} {alarm.Message}")
            .ToArray();
        if (!_lastActiveAlarmSummary.SequenceEqual(latestAlarmSummary))
        {
            ActiveAlarmSummary = latestAlarmSummary;
            _lastActiveAlarmSummary = latestAlarmSummary;
        }
    }
}
