using System.Collections.ObjectModel;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class AlarmViewModel
{
    private readonly Machine _machine;

    public AlarmViewModel(Machine machine)
    {
        _machine = machine;
        ActiveAlarmAxes = new ObservableCollection<string>();
    }

    public ObservableCollection<string> ActiveAlarmAxes { get; }

    public void Refresh()
    {
        ActiveAlarmAxes.Clear();
        foreach (var axis in _machine.Axes.Where(a => a.HasAlarm))
        {
            ActiveAlarmAxes.Add($"{axis.Name} (AxisNo={axis.ControllerAxisNo})");
        }
    }
}
