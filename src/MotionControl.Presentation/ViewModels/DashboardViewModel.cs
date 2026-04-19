using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class DashboardViewModel
{
    private readonly Machine _machine;

    public DashboardViewModel(Machine machine)
    {
        _machine = machine;
    }

    public string SystemState => _machine.CurrentState.ToString();
    public int AxisCount => _machine.Axes.Count;
    public int AlarmCount => _machine.Axes.Count(axis => axis.HasAlarm);

    public void Refresh()
    {
        // 当前骨架阶段通过属性实时读取 Machine 聚合状态。
    }
}
