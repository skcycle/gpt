using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;

namespace MotionControl.Presentation.ViewModels;

public sealed class IoMonitorViewModel : INotifyPropertyChanged
{
    private readonly Machine _machine;
    private readonly IoControlService _ioControlService;
    private IoPointViewModel? _selectedInput;
    private IoPointViewModel? _selectedOutput;

    public IoMonitorViewModel(Machine machine, IoControlService ioControlService)
    {
        _machine = machine;
        _ioControlService = ioControlService;
        Inputs = BuildInputs();
        Outputs = BuildOutputs();
    }

    public ObservableCollection<IoPointViewModel> Inputs { get; private set; }
    public ObservableCollection<IoPointViewModel> Outputs { get; private set; }

    public IoPointViewModel? SelectedInput
    {
        get => _selectedInput;
        set
        {
            if (_selectedInput == value) return;
            _selectedInput = value;
            OnPropertyChanged();
        }
    }

    public IoPointViewModel? SelectedOutput
    {
        get => _selectedOutput;
        set
        {
            if (_selectedOutput == value) return;
            _selectedOutput = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshAll()
    {
        Inputs = BuildInputs();
        Outputs = BuildOutputs();
        OnPropertyChanged(nameof(Inputs));
        OnPropertyChanged(nameof(Outputs));
    }

    public void SelectIoPoint(IoPointViewModel? ioPoint)
    {
        if (ioPoint is null)
        {
            return;
        }

        if (ioPoint.IsOutput)
        {
            SelectedOutput = ioPoint;
        }
        else
        {
            SelectedInput = ioPoint;
        }
    }

    public IReadOnlyList<IoPointViewModel> GetAllIoPoints()
        => Inputs.Concat(Outputs).ToList();

    private ObservableCollection<IoPointViewModel> BuildInputs()
        => new(_machine.IoPoints.Where(io => !io.IsOutput).Select(io => new IoPointViewModel(io, _ioControlService)));

    private ObservableCollection<IoPointViewModel> BuildOutputs()
        => new(_machine.IoPoints.Where(io => io.IsOutput).Select(io => new IoPointViewModel(io, _ioControlService)));

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
