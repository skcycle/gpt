using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisMonitorViewModel : INotifyPropertyChanged
{
    private readonly Machine _machine;
    private AxisViewModel? _selectedAxis;

    public AxisMonitorViewModel(Machine machine, MotionControl.Control.Interfaces.IAxisControlService axisControlService)
    {
        _machine = machine;
        _axisControlService = axisControlService;
        Axes = new ObservableCollection<AxisViewModel>(machine.Axes.Select(axis => new AxisViewModel(axis, axisControlService)));
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
        SyncAxes();

        foreach (var axis in Axes)
        {
            axis.Refresh();
        }
    }

    public AxisViewModel AddAxis(Axis axis)
    {
        var viewModel = new AxisViewModel(axis, GetAxisControlService());
        Axes.Add(viewModel);
        SelectedAxis = viewModel;
        return viewModel;
    }

    public void ReloadFromMachine()
    {
        Axes.Clear();
        foreach (var axis in _machine.Axes)
        {
            Axes.Add(new AxisViewModel(axis, GetAxisControlService()));
        }
        SelectedAxis = Axes.FirstOrDefault();
    }

    public void RemoveAxis(int axisNo)
    {
        var existing = Axes.FirstOrDefault(axis => axis.AxisNo == axisNo);
        if (existing is null)
        {
            return;
        }

        var wasSelected = SelectedAxis == existing;
        Axes.Remove(existing);
        if (wasSelected)
        {
            SelectedAxis = Axes.FirstOrDefault();
        }
    }

    private void SyncAxes()
    {
        var machineAxisNos = _machine.Axes.Select(axis => axis.Id.Value).ToHashSet();
        for (var index = Axes.Count - 1; index >= 0; index--)
        {
            if (!machineAxisNos.Contains(Axes[index].AxisNo))
            {
                Axes.RemoveAt(index);
            }
        }

        foreach (var axis in _machine.Axes)
        {
            if (Axes.All(existing => existing.AxisNo != axis.Id.Value))
            {
                Axes.Add(new AxisViewModel(axis, GetAxisControlService()));
            }
        }
    }

    private readonly MotionControl.Control.Interfaces.IAxisControlService _axisControlService;

    private MotionControl.Control.Interfaces.IAxisControlService GetAxisControlService() => _axisControlService;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
