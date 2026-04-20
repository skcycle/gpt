using System.Collections.ObjectModel;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class IoMonitorViewModel
{
    private readonly Machine _machine;

    public IoMonitorViewModel(Machine machine)
    {
        _machine = machine;
        Inputs = new ObservableCollection<IoPointViewModel>(_machine.IoPoints.Where(io => !io.IsOutput).Select(io => new IoPointViewModel(io)));
        Outputs = new ObservableCollection<IoPointViewModel>(_machine.IoPoints.Where(io => io.IsOutput).Select(io => new IoPointViewModel(io)));
    }

    public ObservableCollection<IoPointViewModel> Inputs { get; }
    public ObservableCollection<IoPointViewModel> Outputs { get; }

    public void RefreshAll()
    {
        foreach (var input in Inputs)
        {
            input.Refresh();
        }

        foreach (var output in Outputs)
        {
            output.Refresh();
        }
    }
}
