using System.Collections.ObjectModel;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(Machine machine)
    {
        Axes = new ObservableCollection<AxisViewModel>(machine.Axes.Select(axis => new AxisViewModel(axis)));
    }

    public ObservableCollection<AxisViewModel> Axes { get; }
}
