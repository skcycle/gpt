using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class MagazineMonitorViewModel : INotifyPropertyChanged
{
    private readonly Machine _machine;
    private readonly IoControlService _ioControlService;
    private readonly Func<bool> _canControl;
    private readonly MagazineEventRuntimeState _magazineEventRuntimeState;
    private MagazineItemViewModel? _selectedMagazine;

    public MagazineMonitorViewModel(Machine machine, IoControlService ioControlService, MagazineEventRuntimeState magazineEventRuntimeState, Func<bool> canControl)
    {
        _machine = machine;
        _ioControlService = ioControlService;
        _magazineEventRuntimeState = magazineEventRuntimeState;
        _canControl = canControl;
        Magazines = new ObservableCollection<MagazineItemViewModel>(_machine.Magazines.Select(BuildViewModel));
    }

    public ObservableCollection<MagazineItemViewModel> Magazines { get; }
    public event Action? SelectedMagazinePositionChanged;

    public MagazineItemViewModel? SelectedMagazine
    {
        get => _selectedMagazine;
        set
        {
            if (_selectedMagazine == value) return;
            if (_selectedMagazine is not null)
            {
                _selectedMagazine.PropertyChanged -= OnSelectedMagazinePropertyChanged;
            }

            _selectedMagazine = value;
            OnPropertyChanged();

            if (value is not null)
            {
                value.PropertyChanged += OnSelectedMagazinePropertyChanged;
                RaiseCanExecuteChanged(value.VacuumCommand);
                RaiseCanExecuteChanged(value.BlowCommand);
            }

            SelectedMagazinePositionChanged?.Invoke();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshAll()
    {
        foreach (var magazine in Magazines) magazine.Refresh();
    }

    public void ReloadFromMachine()
    {
        Magazines.Clear();
        foreach (var magazine in _machine.Magazines)
        {
            Magazines.Add(BuildViewModel(magazine));
        }
        SelectedMagazine = Magazines.FirstOrDefault();
    }

    public void AddMagazine(Magazine magazine)
    {
        var vm = BuildViewModel(magazine);
        Magazines.Add(vm);
        SelectedMagazine = vm;
    }

    public void RemoveMagazine(string name)
    {
        var existing = Magazines.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return;
        Magazines.Remove(existing);
        if (SelectedMagazine == existing) SelectedMagazine = Magazines.FirstOrDefault();
    }

    private MagazineItemViewModel BuildViewModel(Magazine magazine) => new(magazine, _machine, _ioControlService, _magazineEventRuntimeState, _canControl);

    private void OnSelectedMagazinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MagazineItemViewModel.SelectedPosition))
        {
            SelectedMagazinePositionChanged?.Invoke();
        }
    }

    private static void RaiseCanExecuteChanged(System.Windows.Input.ICommand command)
    {
        if (command is MotionControl.Presentation.Commands.RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
