using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Application.Interfaces;
using MotionControl.Domain.Enums;
using MotionControl.Infrastructure.Configuration;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisParameterEditorViewModel : INotifyPropertyChanged
{
    private readonly IAxisParameterAppService _axisParameterAppService;
    private int _axisNo;
    private string _name = string.Empty;
    private string _group = string.Empty;
    private bool _isMaster;
    private string _masterAxisName = string.Empty;
    private double? _softLimitPositive;
    private double? _softLimitNegative;
    private HomeMode _homeMode = HomeMode.Default;
    private string _servoBinding = string.Empty;
    private string _statusMessage = "Ready";

    public AxisParameterEditorViewModel(IAxisParameterAppService axisParameterAppService)
    {
        _axisParameterAppService = axisParameterAppService;
        LoadCommand = new RelayCommand(async () => await LoadAsync(), () => AxisNo >= 0);
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => AxisNo >= 0);
    }

    public int AxisNo
    {
        get => _axisNo;
        set
        {
            _axisNo = value;
            OnPropertyChanged();
            LoadCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Group
    {
        get => _group;
        set { _group = value; OnPropertyChanged(); }
    }

    public bool IsMaster
    {
        get => _isMaster;
        set { _isMaster = value; OnPropertyChanged(); }
    }

    public string MasterAxisName
    {
        get => _masterAxisName;
        set { _masterAxisName = value; OnPropertyChanged(); }
    }

    public double? SoftLimitPositive
    {
        get => _softLimitPositive;
        set { _softLimitPositive = value; OnPropertyChanged(); }
    }

    public double? SoftLimitNegative
    {
        get => _softLimitNegative;
        set { _softLimitNegative = value; OnPropertyChanged(); }
    }

    public HomeMode HomeMode
    {
        get => _homeMode;
        set { _homeMode = value; OnPropertyChanged(); }
    }

    public string ServoBinding
    {
        get => _servoBinding;
        set { _servoBinding = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public Array HomeModes => Enum.GetValues(typeof(HomeMode));
    public RelayCommand LoadCommand { get; }
    public RelayCommand SaveCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task SyncAxisNoAsync(int axisNo)
    {
        AxisNo = axisNo;
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        var item = await _axisParameterAppService.LoadAxisParametersAsync(AxisNo);
        if (item is null)
        {
            StatusMessage = $"Axis {AxisNo} config not found";
            return;
        }

        Name = item.Name;
        Group = item.Group;
        IsMaster = item.IsMaster;
        MasterAxisName = item.MasterAxisName ?? string.Empty;
        SoftLimitPositive = item.SoftLimitPositive;
        SoftLimitNegative = item.SoftLimitNegative;
        HomeMode = item.HomeMode;
        ServoBinding = item.ServoBinding;
        StatusMessage = $"Axis {AxisNo} config loaded";
    }

    public async Task SaveAsync()
    {
        await _axisParameterAppService.SaveAxisParametersAsync(new AxisMappingItem
        {
            AxisNo = AxisNo,
            Name = Name,
            Group = Group,
            IsMaster = IsMaster,
            MasterAxisName = string.IsNullOrWhiteSpace(MasterAxisName) ? null : MasterAxisName,
            SoftLimitPositive = SoftLimitPositive,
            SoftLimitNegative = SoftLimitNegative,
            HomeMode = HomeMode,
            ServoBinding = ServoBinding
        });

        StatusMessage = $"Axis {AxisNo} config saved";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
