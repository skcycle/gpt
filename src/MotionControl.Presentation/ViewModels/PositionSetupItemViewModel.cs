using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Presentation.ViewModels;

public sealed class PositionSetupItemViewModel : INotifyPropertyChanged
{
    private readonly PositionSetupConfigItem _item;

    public PositionSetupItemViewModel(PositionSetupConfigItem item)
    {
        _item = item;
    }

    public string Name { get => _item.Name; set { if (_item.Name == value) return; _item.Name = value; OnPropertyChanged(); } }
    public string Description { get => _item.Description; set { if (_item.Description == value) return; _item.Description = value; OnPropertyChanged(); } }
    public double SafeZ { get => _item.SafeZ; set { if (_item.SafeZ == value) return; _item.SafeZ = value; OnPropertyChanged(); } }
    public int XxAxisNo { get => _item.XxAxisNo; set { if (_item.XxAxisNo == value) return; _item.XxAxisNo = value; OnPropertyChanged(); } }
    public int XAxisNo { get => _item.XAxisNo; set { if (_item.XAxisNo == value) return; _item.XAxisNo = value; OnPropertyChanged(); } }
    public int YAxisNo { get => _item.YAxisNo; set { if (_item.YAxisNo == value) return; _item.YAxisNo = value; OnPropertyChanged(); } }
    public int ZAxisNo { get => _item.ZAxisNo; set { if (_item.ZAxisNo == value) return; _item.ZAxisNo = value; OnPropertyChanged(); } }
    public int UAxisNo { get => _item.UAxisNo; set { if (_item.UAxisNo == value) return; _item.UAxisNo = value; OnPropertyChanged(); } }
    public int VAxisNo { get => _item.VAxisNo; set { if (_item.VAxisNo == value) return; _item.VAxisNo = value; OnPropertyChanged(); } }
    public int WAxisNo { get => _item.WAxisNo; set { if (_item.WAxisNo == value) return; _item.WAxisNo = value; OnPropertyChanged(); } }
    public double XxPosition { get => _item.XxPosition; set { if (_item.XxPosition == value) return; _item.XxPosition = value; OnPropertyChanged(); } }
    public double XPosition { get => _item.XPosition; set { if (_item.XPosition == value) return; _item.XPosition = value; OnPropertyChanged(); } }
    public double YPosition { get => _item.YPosition; set { if (_item.YPosition == value) return; _item.YPosition = value; OnPropertyChanged(); } }
    public double ZPosition { get => _item.ZPosition; set { if (_item.ZPosition == value) return; _item.ZPosition = value; OnPropertyChanged(); } }
    public double UPosition { get => _item.UPosition; set { if (_item.UPosition == value) return; _item.UPosition = value; OnPropertyChanged(); } }
    public double VPosition { get => _item.VPosition; set { if (_item.VPosition == value) return; _item.VPosition = value; OnPropertyChanged(); } }
    public double WPosition { get => _item.WPosition; set { if (_item.WPosition == value) return; _item.WPosition = value; OnPropertyChanged(); } }
    public PositionSetupConfigItem ToConfig() => _item;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
