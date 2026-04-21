using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotionControl.Application.Interfaces;
using MotionControl.Domain.Enums;
using MotionControl.Infrastructure.Configuration;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisParameterEditorViewModel : INotifyPropertyChanged
{
    private readonly IAxisManagementAppService _axisManagementAppService;
    private readonly IAxisControllerParameterAppService _axisControllerParameterAppService;
    private int _axisNo;
    private string _name = string.Empty;
    private string _group = string.Empty;
    private bool _isMaster;
    private string _masterAxisName = string.Empty;
    private double? _softLimitPositive;
    private double? _softLimitNegative;
    private double? _workVelocity;
    private double? _setupVelocity;
    private double? _pulseEquivalent = 1000;
    private HomeMode _homeMode = HomeMode.Default;
    private string _servoBinding = string.Empty;
    private string _statusMessage = "Ready";
    private string _controllerParameterSnapshot = string.Empty;

    public AxisParameterEditorViewModel(IAxisManagementAppService axisManagementAppService, IAxisControllerParameterAppService axisControllerParameterAppService)
    {
        _axisManagementAppService = axisManagementAppService;
        _axisControllerParameterAppService = axisControllerParameterAppService;
        LoadCommand = new RelayCommand(async () => await LoadAsync(), () => AxisNo >= 0);
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => AxisNo >= 0);
        ReadControllerCommand = new RelayCommand(async () => await ReadControllerAsync(), () => AxisNo >= 0);
        WriteControllerCommand = new RelayCommand(async () => await WriteControllerAsync(), () => AxisNo >= 0);
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

    public double? WorkVelocity
    {
        get => _workVelocity;
        set { _workVelocity = value; OnPropertyChanged(); }
    }

    public double? SetupVelocity
    {
        get => _setupVelocity;
        set { _setupVelocity = value; OnPropertyChanged(); }
    }

    public double? PulseEquivalent
    {
        get => _pulseEquivalent;
        set { _pulseEquivalent = value; OnPropertyChanged(); }
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

    public string ControllerParameterSnapshot
    {
        get => _controllerParameterSnapshot;
        set { _controllerParameterSnapshot = value; OnPropertyChanged(); }
    }

    public Array HomeModes => Enum.GetValues(typeof(HomeMode));
    public RelayCommand LoadCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand ReadControllerCommand { get; }
    public RelayCommand WriteControllerCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task SyncAxisNoAsync(int axisNo)
    {
        AxisNo = axisNo;
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        var item = await _axisManagementAppService.LoadAxisAsync(AxisNo);
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
        WorkVelocity = item.WorkVelocity;
        SetupVelocity = item.SetupVelocity;
        PulseEquivalent = item.PulseEquivalent ?? 1000;
        HomeMode = item.HomeMode;
        ServoBinding = item.ServoBinding;
        StatusMessage = $"Axis {AxisNo} config loaded and applied";
    }

    public async Task SaveAsync()
    {
        var item = new AxisMappingItem
        {
            AxisNo = AxisNo,
            Name = Name,
            Group = Group,
            IsMaster = IsMaster,
            MasterAxisName = string.IsNullOrWhiteSpace(MasterAxisName) ? null : MasterAxisName,
            SoftLimitPositive = SoftLimitPositive,
            SoftLimitNegative = SoftLimitNegative,
            WorkVelocity = WorkVelocity,
            SetupVelocity = SetupVelocity,
            PulseEquivalent = PulseEquivalent,
            HomeMode = HomeMode,
            ServoBinding = ServoBinding
        };

        await _axisManagementAppService.SaveAxisAsync(item);

        StatusMessage = $"Axis {AxisNo} config saved and applied";
    }

    public async Task ReadControllerAsync()
    {
        ControllerParameterSnapshot = await _axisControllerParameterAppService.ReadControllerParametersAsync(AxisNo);
        StatusMessage = $"Axis {AxisNo} controller parameters read";
    }

    public async Task WriteControllerAsync()
    {
        await _axisControllerParameterAppService.WriteControllerParametersAsync(
            AxisNo,
            WorkVelocity ?? 0,
            SetupVelocity ?? 0,
            PulseEquivalent ?? 1000);
        StatusMessage = $"Axis {AxisNo} controller parameters written";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
