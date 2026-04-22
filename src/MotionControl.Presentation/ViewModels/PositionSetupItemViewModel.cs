using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Infrastructure.Configuration;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

/// <summary>
/// 封装 PositionSetupConfigItem（父对象）。
/// 包含轴映射、SafeZ，以及该对象下所有具名位置点。
/// 每个 PositionSetupItemViewModel 有自己的 AddPosition / DeleteSelectedPosition 方法。
/// </summary>
public sealed class PositionSetupItemViewModel : INotifyPropertyChanged
{
    private readonly PositionSetupConfigItem _item;

    public PositionSetupItemViewModel(PositionSetupConfigItem item)
    {
        _item = item;
        foreach (var pos in item.Positions)
        {
            _positions.Add(new PositionSetupPositionViewModel(pos));
        }
    }

    public PositionSetupConfigItem ToConfig()
    {
        _item.Positions = _positions.Select(p => p.ToConfig()).ToList();
        return _item;
    }

    // ===== 父对象字段（轴映射、SafeZ） =====

    public string Name { get => _item.Name; set { if (_item.Name == value) return; _item.Name = value; OnPropertyChanged(); } }
    public string Description { get => _item.Description; set { if (_item.Description == value) return; _item.Description = value; OnPropertyChanged(); } }

    /// <summary>安全抬升 Z 位移（该对象下所有位置共用）</summary>
    public double SafeZ { get => _item.SafeZ; set { if (_item.SafeZ == value) return; _item.SafeZ = value; OnPropertyChanged(); } }

    /// <summary>轴映射（该对象下所有位置共用）</summary>
    public int XxAxisNo { get => _item.XxAxisNo; set { if (_item.XxAxisNo == value) return; _item.XxAxisNo = value; OnPropertyChanged(); } }
    public int XAxisNo { get => _item.XAxisNo; set { if (_item.XAxisNo == value) return; _item.XAxisNo = value; OnPropertyChanged(); } }
    public int YAxisNo { get => _item.YAxisNo; set { if (_item.YAxisNo == value) return; _item.YAxisNo = value; OnPropertyChanged(); } }
    public int ZAxisNo { get => _item.ZAxisNo; set { if (_item.ZAxisNo == value) return; _item.ZAxisNo = value; OnPropertyChanged(); } }
    public int UAxisNo { get => _item.UAxisNo; set { if (_item.UAxisNo == value) return; _item.UAxisNo = value; OnPropertyChanged(); } }
    public int VAxisNo { get => _item.VAxisNo; set { if (_item.VAxisNo == value) return; _item.VAxisNo = value; OnPropertyChanged(); } }
    public int WAxisNo { get => _item.WAxisNo; set { if (_item.WAxisNo == value) return; _item.WAxisNo = value; OnPropertyChanged(); } }

    // ===== 子位置点集合 =====

    private readonly ObservableCollection<PositionSetupPositionViewModel> _positions = new();
    public ObservableCollection<PositionSetupPositionViewModel> Positions => _positions;

    private PositionSetupPositionViewModel? _selectedPosition;
    public PositionSetupPositionViewModel? SelectedPosition
    {
        get => _selectedPosition;
        set
        {
            if (_selectedPosition == value) return;
            _selectedPosition = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedPosition));
            (DeletePositionCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (TeachPositionCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (MovePositionCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool HasSelectedPosition => _selectedPosition is not null;

    public void AddPosition()
    {
        var pos = new PositionSetupPositionConfigItem
        {
            Name = $"Position {_positions.Count + 1}",
            Description = string.Empty
        };
        _item.Positions.Add(pos);
        var vm = new PositionSetupPositionViewModel(pos);
        _positions.Add(vm);
        SelectedPosition = vm;
    }

    public void DeleteSelectedPosition()
    {
        if (SelectedPosition is null) return;
        var pos = SelectedPosition;
        _positions.Remove(pos);
        _item.Positions.Remove(pos.ToConfig());
        SelectedPosition = _positions.FirstOrDefault();
    }

    /// <summary>
    /// 刷新所有绑定（供 MainWindowViewModel 在 Teach/Move 成功后调用）。
    /// </summary>
    public void Refresh()
    {
        OnPropertyChanged(string.Empty);
    }

    // 内部命令（由 MainWindowViewModel 注入，Teach/Move 操作 SelectedPosition）
    public RelayCommand? DeletePositionCommand { get; set; }
    public RelayCommand? TeachPositionCommand { get; set; }
    public RelayCommand? MovePositionCommand { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
