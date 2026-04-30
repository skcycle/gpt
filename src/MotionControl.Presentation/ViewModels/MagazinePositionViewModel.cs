using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Presentation.ViewModels;

public sealed class MagazinePositionViewModel : INotifyPropertyChanged
{
    private readonly MagazinePositionConfigItem _item;

    public MagazinePositionViewModel(MagazinePositionConfigItem item)
    {
        _item = item;
    }

    public string Name
    {
        get => _item.Name;
        set
        {
            if (IsSystemDefault) return;
            if (_item.Name == value) return;
            _item.Name = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _item.Description;
        set { if (_item.Description == value) return; _item.Description = value; OnPropertyChanged(); }
    }

    public string Kind
    {
        get => _item.Kind;
        set { if (_item.Kind == value) return; _item.Kind = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSystemDefault)); OnPropertyChanged(nameof(BadgeText)); OnPropertyChanged(nameof(BadgeBrush)); }
    }

    public double X
    {
        get => _item.X;
        set { if (_item.X == value) return; _item.X = value; OnPropertyChanged(); }
    }

    public double Y
    {
        get => _item.Y;
        set { if (_item.Y == value) return; _item.Y = value; OnPropertyChanged(); }
    }

    public double Z
    {
        get => _item.Z;
        set { if (_item.Z == value) return; _item.Z = value; OnPropertyChanged(); }
    }

    public bool IsSystemDefault =>
        string.Equals(_item.Kind, MagazinePositionKinds.PickStart, StringComparison.OrdinalIgnoreCase)
        || string.Equals(_item.Kind, MagazinePositionKinds.InspectStart, StringComparison.OrdinalIgnoreCase);

    public string BadgeText => Kind switch
    {
        MagazinePositionKinds.PickStart => "取料起始位",
        MagazinePositionKinds.InspectStart => "检测起始位",
        _ => string.Empty
    };

    public Brush BadgeBrush => Kind switch
    {
        MagazinePositionKinds.PickStart => new SolidColorBrush(Color.FromRgb(74, 158, 255)),
        MagazinePositionKinds.InspectStart => new SolidColorBrush(Color.FromRgb(155, 89, 182)),
        _ => Brushes.Transparent
    };

    public MagazinePositionConfigItem ToConfig() => _item;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
