using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class MagazineItemViewModel : INotifyPropertyChanged
{
    private readonly Magazine _magazine;
    private readonly Machine _machine;
    private MagazinePositionViewModel? _selectedPosition;
    private string _currentLayerStatusText = "未配置";
    private Brush _currentLayerStatusBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));

    public MagazineItemViewModel(Magazine magazine, Machine machine)
    {
        _magazine = magazine;
        _machine = machine;
        Positions = new ObservableCollection<MagazinePositionViewModel>(_magazine.Positions.Select(position => new MagazinePositionViewModel(position)));
        _selectedPosition = Positions.FirstOrDefault(position => string.Equals(position.Name, _magazine.SelectedPositionName, StringComparison.OrdinalIgnoreCase)) ?? Positions.FirstOrDefault();
        Refresh();
    }

    public string Name { get => _magazine.Name; set { if (_magazine.Name == value) return; UpdateMetadata(name: value); OnPropertyChanged(); } }
    public string Description { get => _magazine.Description; set { if (_magazine.Description == value) return; UpdateMetadata(description: value); OnPropertyChanged(); } }
    public int XAxisNo { get => _magazine.XAxisNo; set { if (_magazine.XAxisNo == value) return; UpdateMetadata(xAxisNo: value); OnPropertyChanged(); } }
    public int YAxisNo { get => _magazine.YAxisNo; set { if (_magazine.YAxisNo == value) return; UpdateMetadata(yAxisNo: value); OnPropertyChanged(); } }
    public int ZAxisNo { get => _magazine.ZAxisNo; set { if (_magazine.ZAxisNo == value) return; UpdateMetadata(zAxisNo: value); OnPropertyChanged(); } }
    public int MaterialPresentInputAddress { get => _magazine.MaterialPresentInputAddress; set { if (_magazine.MaterialPresentInputAddress == value) return; UpdateMetadata(materialPresentInputAddress: value); OnPropertyChanged(); } }
    public int CurrentLayerHasMaterialInputAddress { get => _magazine.CurrentLayerHasMaterialInputAddress; set { if (_magazine.CurrentLayerHasMaterialInputAddress == value) return; UpdateMetadata(currentLayerHasMaterialInputAddress: value); OnPropertyChanged(); } }
    public int TrayKeyingInputAddress { get => _magazine.TrayKeyingInputAddress; set { if (_magazine.TrayKeyingInputAddress == value) return; UpdateMetadata(trayKeyingInputAddress: value); OnPropertyChanged(); } }
    public int LayerCount { get => _magazine.LayerCount; set { if (_magazine.LayerCount == value) return; UpdateMetadata(layerCount: value); OnPropertyChanged(); } }
    public double LayerHeight { get => _magazine.LayerHeight; set { if (_magazine.LayerHeight == value) return; UpdateMetadata(layerHeight: value); OnPropertyChanged(); } }
    public double PickLiftHeight { get => _magazine.PickLiftHeight; set { if (_magazine.PickLiftHeight == value) return; UpdateMetadata(pickLiftHeight: value); OnPropertyChanged(); } }
    public int ScanSettlingMs { get => _magazine.ScanSettlingMs; set { if (_magazine.ScanSettlingMs == value) return; UpdateMetadata(scanSettlingMs: value); OnPropertyChanged(); } }

    public ObservableCollection<MagazinePositionViewModel> Positions { get; }
    public string CurrentLayerStatusText => _currentLayerStatusText;
    public Brush CurrentLayerStatusBrush => _currentLayerStatusBrush;

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
        var (text, brush) = ResolveCurrentLayerStatus();
        if (_currentLayerStatusText != text)
        {
            _currentLayerStatusText = text;
            OnPropertyChanged(nameof(CurrentLayerStatusText));
        }

        if (!Equals(_currentLayerStatusBrush, brush))
        {
            _currentLayerStatusBrush = brush;
            OnPropertyChanged(nameof(CurrentLayerStatusBrush));
        }
    }

    private (string Text, Brush Brush) ResolveCurrentLayerStatus()
    {
        if (_magazine.CurrentLayerHasMaterialInputAddress < 0)
        {
            return ("未配置", new SolidColorBrush(Color.FromRgb(90, 90, 90)));
        }

        var io = _machine.IoPoints.FirstOrDefault(point => !point.IsOutput && point.Address == _magazine.CurrentLayerHasMaterialInputAddress);
        if (io is null)
        {
            return ("未知", new SolidColorBrush(Color.FromRgb(160, 120, 40)));
        }

        return io.Value
            ? ("有料", new SolidColorBrush(Color.FromRgb(0, 200, 0)))
            : ("无料", new SolidColorBrush(Color.FromRgb(100, 100, 100)));
    }

    private void UpdateMetadata(
        string? name = null,
        string? description = null,
        int? xAxisNo = null,
        int? yAxisNo = null,
        int? zAxisNo = null,
        int? materialPresentInputAddress = null,
        int? currentLayerHasMaterialInputAddress = null,
        int? trayKeyingInputAddress = null,
        int? layerCount = null,
        double? layerHeight = null,
        double? pickLiftHeight = null,
        int? scanSettlingMs = null)
    {
        _magazine.UpdateMetadata(
            name ?? _magazine.Name,
            description ?? _magazine.Description,
            xAxisNo ?? _magazine.XAxisNo,
            yAxisNo ?? _magazine.YAxisNo,
            zAxisNo ?? _magazine.ZAxisNo,
            materialPresentInputAddress ?? _magazine.MaterialPresentInputAddress,
            currentLayerHasMaterialInputAddress ?? _magazine.CurrentLayerHasMaterialInputAddress,
            trayKeyingInputAddress ?? _magazine.TrayKeyingInputAddress,
            layerCount ?? _magazine.LayerCount,
            layerHeight ?? _magazine.LayerHeight,
            pickLiftHeight ?? _magazine.PickLiftHeight,
            scanSettlingMs ?? _magazine.ScanSettlingMs);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
