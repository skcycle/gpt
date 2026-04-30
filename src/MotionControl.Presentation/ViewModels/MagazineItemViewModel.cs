using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class MagazineItemViewModel : INotifyPropertyChanged
{
    private readonly Magazine _magazine;
    private readonly Machine _machine;
    private readonly IoControlService _ioControlService;
    private readonly Func<bool> _canControl;
    private readonly MagazineEventRuntimeState _magazineEventRuntimeState;
    private MagazinePositionViewModel? _selectedPosition;

    public MagazineItemViewModel(Magazine magazine, Machine machine, IoControlService ioControlService, MagazineEventRuntimeState magazineEventRuntimeState, Func<bool> canControl)
    {
        _magazine = magazine;
        _machine = machine;
        _ioControlService = ioControlService;
        _magazineEventRuntimeState = magazineEventRuntimeState;
        _canControl = canControl;
        Positions = new ObservableCollection<MagazinePositionViewModel>(_magazine.Positions.Select(position => new MagazinePositionViewModel(position)));
        _selectedPosition = Positions.FirstOrDefault(position => string.Equals(position.Name, _magazine.SelectedPositionName, StringComparison.OrdinalIgnoreCase)) ?? Positions.FirstOrDefault();
        VacuumCommand = new RelayCommand(async () => await ToggleVacuumAsync(), () => _canControl() && _magazine.VacuumOutputAddress >= 0);
        BlowCommand = new RelayCommand(async () => await ToggleBlowAsync(), () => _canControl() && _magazine.BlowOutputAddress >= 0);
    }

    public string Name { get => _magazine.Name; set { if (_magazine.Name == value) return; UpdateMetadata(name: value); OnPropertyChanged(); } }
    public string Description { get => _magazine.Description; set { if (_magazine.Description == value) return; UpdateMetadata(description: value); OnPropertyChanged(); } }
    public int XAxisNo { get => _magazine.XAxisNo; set { if (_magazine.XAxisNo == value) return; UpdateMetadata(xAxisNo: value); OnPropertyChanged(); } }
    public int YAxisNo { get => _magazine.YAxisNo; set { if (_magazine.YAxisNo == value) return; UpdateMetadata(yAxisNo: value); OnPropertyChanged(); } }
    public int ZAxisNo { get => _magazine.ZAxisNo; set { if (_magazine.ZAxisNo == value) return; UpdateMetadata(zAxisNo: value); OnPropertyChanged(); } }
    public int VacuumOutputAddress { get => _magazine.VacuumOutputAddress; set { if (_magazine.VacuumOutputAddress == value) return; UpdateMetadata(vacuumOutputAddress: value); OnPropertyChanged(); RaiseCanExecuteChanged(VacuumCommand); RaiseCanExecuteChanged(BlowCommand); } }
    public int BlowOutputAddress { get => _magazine.BlowOutputAddress; set { if (_magazine.BlowOutputAddress == value) return; UpdateMetadata(blowOutputAddress: value); OnPropertyChanged(); RaiseCanExecuteChanged(BlowCommand); } }
    public int MaterialPresentInputAddress { get => _magazine.MaterialPresentInputAddress; set { if (_magazine.MaterialPresentInputAddress == value) return; UpdateMetadata(materialPresentInputAddress: value); OnPropertyChanged(); } }
    public int CurrentLayerHasMaterialInputAddress { get => _magazine.CurrentLayerHasMaterialInputAddress; set { if (_magazine.CurrentLayerHasMaterialInputAddress == value) return; UpdateMetadata(currentLayerHasMaterialInputAddress: value); OnPropertyChanged(); } }
    public int TrayKeyingInputAddress { get => _magazine.TrayKeyingInputAddress; set { if (_magazine.TrayKeyingInputAddress == value) return; UpdateMetadata(trayKeyingInputAddress: value); OnPropertyChanged(); } }
    public int LayerCount { get => _magazine.LayerCount; set { if (_magazine.LayerCount == value) return; UpdateMetadata(layerCount: value); OnPropertyChanged(); } }
    public double LayerHeight { get => _magazine.LayerHeight; set { if (_magazine.LayerHeight == value) return; UpdateMetadata(layerHeight: value); OnPropertyChanged(); } }
    public double PickLiftHeight { get => _magazine.PickLiftHeight; set { if (_magazine.PickLiftHeight == value) return; UpdateMetadata(pickLiftHeight: value); OnPropertyChanged(); } }
    public int ActionTimeoutMs { get => _magazine.ActionTimeoutMs; set { if (_magazine.ActionTimeoutMs == value) return; UpdateMetadata(actionTimeoutMs: value); OnPropertyChanged(); } }

    public ObservableCollection<MagazinePositionViewModel> Positions { get; }

    public MagazinePositionViewModel? SelectedPosition
    {
        get => _selectedPosition ?? Positions.FirstOrDefault();
        set
        {
            if (_selectedPosition == value) return;
            _selectedPosition = value;
            _magazine.SelectedPositionName = value?.Name;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedPosition));
            OnPropertyChanged(nameof(CanDeleteSelectedPosition));
            OnPropertyChanged(nameof(SelectedPositionName));
        }
    }

    public bool HasSelectedPosition => SelectedPosition is not null;
    public bool CanDeleteSelectedPosition => SelectedPosition is not null && !SelectedPosition.IsSystemDefault;
    public string SelectedPositionName
    {
        get => SelectedPosition?.Name ?? string.Empty;
        set
        {
            if (SelectedPosition is null || SelectedPosition.IsSystemDefault) return;
            if (SelectedPosition.Name == value) return;
            SelectedPosition.Name = value;
            _magazine.SelectedPositionName = value;
            OnPropertyChanged();
        }
    }

    public bool VacuumDoOn { get; private set; }
    public bool BlowDoOn { get; private set; }
    public bool MaterialPresentOn { get; private set; }
    public bool CurrentLayerHasMaterialOn { get; private set; }
    public bool TrayKeyingOn { get; private set; }

    public Brush VacuumDoBrush => VacuumDoOn ? OnBrush : OffBrush;
    public Brush BlowDoBrush => BlowDoOn ? OnBrush : OffBrush;
    public Brush MaterialPresentBrush => MaterialPresentOn ? OnBrush : OffBrush;
    public Brush CurrentLayerHasMaterialBrush => CurrentLayerHasMaterialOn ? OnBrush : OffBrush;
    public Brush TrayKeyingBrush => TrayKeyingOn ? OnBrush : OffBrush;

    public ICommand VacuumCommand { get; }
    public ICommand BlowCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void AddPosition()
    {
        var index = Positions.Count(position => !position.IsSystemDefault) + 1;
        var position = new MagazinePosition($"Position {index}", string.Empty, MagazinePositionKinds.Normal, 0, 0, 0);

        _magazine.AddPosition(position);
        var vm = new MagazinePositionViewModel(_magazine.Positions.Last());
        Positions.Add(vm);
        SelectedPosition = vm;
        OnPropertyChanged(nameof(CanDeleteSelectedPosition));
    }

    public bool DeleteSelectedPosition()
    {
        var selected = SelectedPosition;
        if (selected is null || selected.IsSystemDefault) return false;

        var removed = _magazine.DeleteSelectedPosition();
        if (!removed) return false;

        Positions.Remove(selected);
        SelectedPosition = Positions.FirstOrDefault(position => string.Equals(position.Name, _magazine.SelectedPositionName, StringComparison.OrdinalIgnoreCase)) ?? Positions.FirstOrDefault();
        OnPropertyChanged(nameof(CanDeleteSelectedPosition));
        return true;
    }

    public void Refresh()
    {
        VacuumDoOn = ReadOutput(_magazine.VacuumOutputAddress);
        BlowDoOn = ReadOutput(_magazine.BlowOutputAddress);
        MaterialPresentOn = ReadInput(_magazine.MaterialPresentInputAddress);
        CurrentLayerHasMaterialOn = ReadInput(_magazine.CurrentLayerHasMaterialInputAddress);
        TrayKeyingOn = ReadInput(_magazine.TrayKeyingInputAddress);

        OnPropertyChanged(nameof(VacuumDoOn));
        OnPropertyChanged(nameof(BlowDoOn));
        OnPropertyChanged(nameof(MaterialPresentOn));
        OnPropertyChanged(nameof(CurrentLayerHasMaterialOn));
        OnPropertyChanged(nameof(TrayKeyingOn));
        OnPropertyChanged(nameof(VacuumDoBrush));
        OnPropertyChanged(nameof(BlowDoBrush));
        OnPropertyChanged(nameof(MaterialPresentBrush));
        OnPropertyChanged(nameof(CurrentLayerHasMaterialBrush));
        OnPropertyChanged(nameof(TrayKeyingBrush));

        EvaluateVacuumRuntime();
        RaiseCanExecuteChanged(VacuumCommand);
        RaiseCanExecuteChanged(BlowCommand);
    }

    private async Task ToggleVacuumAsync()
    {
        var next = !VacuumDoOn;
        _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Command", Message = $"{_magazine.Name} vacuum {(next ? "on" : "off")} started" });
        var result = await _ioControlService.SetOutputAsync(_magazine.VacuumOutputAddress, next);
        if (!result.Success)
        {
            var code = $"MAG-{_magazine.Name}-VACUUM-COMMAND-FAILED";
            var message = $"{_magazine.Name} vacuum command failed: {result.ErrorMessage}";
            _machine.UpsertAlarm(code, message, _magazine.Name, "Magazine", "Error");
            _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Failed", Message = message });
            return;
        }

        _magazine.StartVacuumCommand();
        _machine.ClearAlarm($"MAG-{_magazine.Name}-VACUUM-COMMAND-FAILED");
        _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Success", Message = $"{_magazine.Name} vacuum {(next ? "on" : "off")} completed" });
    }

    private async Task ToggleBlowAsync()
    {
        var next = !BlowDoOn;
        _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Command", Message = $"{_magazine.Name} blow {(next ? "on" : "off")} started" });
        var result = await _ioControlService.SetOutputAsync(_magazine.BlowOutputAddress, next);
        if (!result.Success)
        {
            var code = $"MAG-{_magazine.Name}-BLOW-COMMAND-FAILED";
            var message = $"{_magazine.Name} blow command failed: {result.ErrorMessage}";
            _machine.UpsertAlarm(code, message, _magazine.Name, "Magazine", "Error");
            _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Failed", Message = message });
            return;
        }

        _machine.ClearAlarm($"MAG-{_magazine.Name}-BLOW-COMMAND-FAILED");
        _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Success", Message = $"{_magazine.Name} blow {(next ? "on" : "off")} completed" });
    }

    private void UpdateMetadata(
        string? name = null,
        string? description = null,
        int? xAxisNo = null,
        int? yAxisNo = null,
        int? zAxisNo = null,
        int? vacuumOutputAddress = null,
        int? blowOutputAddress = null,
        int? materialPresentInputAddress = null,
        int? currentLayerHasMaterialInputAddress = null,
        int? trayKeyingInputAddress = null,
        int? layerCount = null,
        double? layerHeight = null,
        double? pickLiftHeight = null,
        int? actionTimeoutMs = null)
    {
        _magazine.UpdateMetadata(
            name ?? _magazine.Name,
            description ?? _magazine.Description,
            xAxisNo ?? _magazine.XAxisNo,
            yAxisNo ?? _magazine.YAxisNo,
            zAxisNo ?? _magazine.ZAxisNo,
            vacuumOutputAddress ?? _magazine.VacuumOutputAddress,
            blowOutputAddress ?? _magazine.BlowOutputAddress,
            materialPresentInputAddress ?? _magazine.MaterialPresentInputAddress,
            currentLayerHasMaterialInputAddress ?? _magazine.CurrentLayerHasMaterialInputAddress,
            trayKeyingInputAddress ?? _magazine.TrayKeyingInputAddress,
            layerCount ?? _magazine.LayerCount,
            layerHeight ?? _magazine.LayerHeight,
            pickLiftHeight ?? _magazine.PickLiftHeight,
            actionTimeoutMs ?? _magazine.ActionTimeoutMs);
    }

    private void EvaluateVacuumRuntime()
    {
        var timeoutAlarmCode = $"MAG-{_magazine.Name}-VACUUM-TIMEOUT";
        var conflictAlarmCode = $"MAG-{_magazine.Name}-VACUUM-CONFLICT";

        if (VacuumDoOn && MaterialPresentOn && _magazine.PendingVacuumCommand && !_magazine.VacuumSuccessLogged)
        {
            _magazine.VacuumSuccessLogged = true;
            _magazine.StopVacuumCommand();
            _machine.ClearAlarm(timeoutAlarmCode);
            _machine.ClearAlarm(conflictAlarmCode);
            _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Success", Message = $"{_magazine.Name} vacuum sensor confirmed" });
        }
        else if (VacuumDoOn && !MaterialPresentOn && _magazine.HasVacuumTimedOut(DateTime.UtcNow) && !_magazine.VacuumTimeoutLogged)
        {
            _magazine.VacuumTimeoutLogged = true;
            var message = $"{_magazine.Name} vacuum timeout";
            _machine.UpsertAlarm(timeoutAlarmCode, message, _magazine.Name, "Magazine", "Error");
            _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Failed", Message = message });
        }

        if (VacuumDoOn && BlowDoOn && !_magazine.VacuumConflictLogged)
        {
            _magazine.VacuumConflictLogged = true;
            var message = $"{_magazine.Name} vacuum and blow are on at the same time";
            _machine.UpsertAlarm(conflictAlarmCode, message, _magazine.Name, "Magazine", "Error");
            _magazineEventRuntimeState.Add(new MagazineEventRecord { MagazineName = _magazine.Name, EventType = "Failed", Message = message });
        }
        else if ((!VacuumDoOn || !BlowDoOn) && _machine.ClearAlarm(conflictAlarmCode))
        {
            _magazine.VacuumConflictLogged = false;
        }
    }

    private bool ReadOutput(int address) => address >= 0 && _machine.IoPoints.FirstOrDefault(point => point.IsOutput && point.Address == address)?.Value == true;
    private bool ReadInput(int address) => address >= 0 && _machine.IoPoints.FirstOrDefault(point => !point.IsOutput && point.Address == address)?.Value == true;

    private static readonly Brush OnBrush = new SolidColorBrush(Color.FromRgb(0, 200, 0));
    private static readonly Brush OffBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));

    private static void RaiseCanExecuteChanged(ICommand command)
    {
        if (command is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
