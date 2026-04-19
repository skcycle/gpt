using System.Collections.ObjectModel;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisMonitorViewModel
{
    public AxisMonitorViewModel(Machine machine)
    {
        Axes = new ObservableCollection<AxisViewModel>(machine.Axes.Select(axis => new AxisViewModel(axis)));
    }

    public ObservableCollection<AxisViewModel> Axes { get; }

    public void RefreshAll()
    {
        foreach (var axis in Axes)
        {
            axis.Refresh();
        }
    }
}
