using MotionControl.Control.Services;
using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class DashboardViewModel
{
    private readonly Machine _machine;
    private readonly CommandFeedbackRuntimeState _commandFeedbackRuntimeState;
    private EtherCatControllerStatus? _controllerStatus;

    public DashboardViewModel(Machine machine, CommandFeedbackRuntimeState commandFeedbackRuntimeState)
    {
        _machine = machine;
        _commandFeedbackRuntimeState = commandFeedbackRuntimeState;
    }

    public string SystemState => _machine.CurrentState.ToString();
    public int AxisCount => _machine.Axes.Count;
    public int AlarmCount => _machine.Axes.Count(axis => axis.HasAlarm);
    public string EtherCatNetworkState => _controllerStatus?.NetworkState ?? "Unknown";
    public bool EtherCatConnected => _controllerStatus?.IsConnected ?? false;
    public int EtherCatOnlineSlaveCount => _controllerStatus?.OnlineSlaveCount ?? 0;
    public IReadOnlyList<EtherCatSlaveViewModel> EtherCatSlaves { get; private set; } = Array.Empty<EtherCatSlaveViewModel>();
    public IReadOnlyList<string> RecentCommandFeedback { get; private set; } = Array.Empty<string>();

    public void Refresh(EtherCatControllerStatus? controllerStatus = null)
    {
        _controllerStatus = controllerStatus ?? _controllerStatus;
        EtherCatSlaves = _controllerStatus?.Slaves.Select(slave => new EtherCatSlaveViewModel(slave)).ToArray()
            ?? Array.Empty<EtherCatSlaveViewModel>();
        RecentCommandFeedback = _commandFeedbackRuntimeState.RecentFeedback
            .Reverse()
            .Take(5)
            .Select(item => $"[{item.Status}] {item.CommandName} Axis={item.AxisNo?.ToString() ?? "-"} {item.Message}")
            .ToArray();
    }
}
