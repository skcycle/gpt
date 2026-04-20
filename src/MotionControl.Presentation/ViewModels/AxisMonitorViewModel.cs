using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisMonitorViewModel : INotifyPropertyChanged
{
    private AxisViewModel? _selectedAxis;

    public AxisMonitorViewModel(Machine machine)
    {
        Axes = new ObservableCollection<AxisViewModel>(machine.Axes.Select(axis => new AxisViewModel(axis)));
        _selectedAxis = Axes.FirstOrDefault();
    }

    public ObservableCollection<AxisViewModel> Axes { get; }

    public AxisViewModel? SelectedAxis
    {
        get => _selectedAxis;
        set
        {
            if (_selectedAxis == value)
            {
                return;
            }

            _selectedAxis = value;
            OnPropertyChanged();
            SelectedAxisChanged?.Invoke(value);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<AxisViewModel?>? SelectedAxisChanged;

    public void RefreshAll()
    {
        foreach (var axis in Axes)
        {
            axis.Refresh();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
