using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Presentation.ViewModels;

public sealed class PositionSetupMonitorViewModel : INotifyPropertyChanged
{
    public ObservableCollection<PositionSetupItemViewModel> Positions { get; } = new();

    private PositionSetupItemViewModel? _selectedPosition;
    public PositionSetupItemViewModel? SelectedPosition
    {
        get => _selectedPosition;
        set
        {
            if (_selectedPosition == value) return;
            _selectedPosition = value;
            OnPropertyChanged();
        }
    }

    public void Load(IEnumerable<PositionSetupConfigItem> items)
    {
        Positions.Clear();
        foreach (var item in items)
        {
            Positions.Add(new PositionSetupItemViewModel(item));
        }
        SelectedPosition = Positions.FirstOrDefault();
    }

    public void Add(PositionSetupConfigItem item)
    {
        var vm = new PositionSetupItemViewModel(item);
        Positions.Add(vm);
        SelectedPosition = vm;
    }

    public void RemoveSelected()
    {
        if (SelectedPosition is null) return;
        var current = SelectedPosition;
        Positions.Remove(current);
        SelectedPosition = Positions.FirstOrDefault();
    }

    public IReadOnlyList<PositionSetupConfigItem> ToConfigs() => Positions.Select(item => item.ToConfig()).ToList();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
