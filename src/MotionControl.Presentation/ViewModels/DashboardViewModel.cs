using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class DashboardViewModel
{
    private readonly Machine _machine;
    private EtherCatControllerStatus? _controllerStatus;

    public DashboardViewModel(Machine machine)
    {
        _machine = machine;
    }

    public string SystemState => _machine.CurrentState.ToString();
    public int AxisCount => _machine.Axes.Count;
    public int AlarmCount => _machine.Axes.Count(axis => axis.HasAlarm);
    public string EtherCatNetworkState => _controllerStatus?.NetworkState ?? "Unknown";
    public bool EtherCatConnected => _controllerStatus?.IsConnected ?? false;
    public int EtherCatOnlineSlaveCount => _controllerStatus?.OnlineSlaveCount ?? 0;

    public void Refresh(EtherCatControllerStatus? controllerStatus = null)
    {
        _controllerStatus = controllerStatus ?? _controllerStatus;
    }
}
