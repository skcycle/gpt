using System.Text.Json;
using System.Text.Json.Serialization;
using MotionControl.Application.Interfaces;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class AxisParameterAppService(string appSettingsPath) : IAxisParameterAppService
{
    public async Task<AxisMappingItem?> LoadAxisParametersAsync(int axisNo, CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        return root.AxisMapping.Axes.FirstOrDefault(axis => axis.AxisNo == axisNo);
    }

    public async Task SaveAxisParametersAsync(AxisMappingItem axisMappingItem, CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        var existing = root.AxisMapping.Axes.FirstOrDefault(axis => axis.AxisNo == axisMappingItem.AxisNo);
        if (existing is null)
        {
            root.AxisMapping.Axes.Add(axisMappingItem);
        }
        else
        {
            existing.Name = axisMappingItem.Name;
            existing.Group = axisMappingItem.Group;
            existing.IsMaster = axisMappingItem.IsMaster;
            existing.MasterAxisName = axisMappingItem.MasterAxisName;
            existing.SoftLimitPositive = axisMappingItem.SoftLimitPositive;
            existing.SoftLimitNegative = axisMappingItem.SoftLimitNegative;
            existing.WorkVelocity = axisMappingItem.WorkVelocity;
            existing.SetupVelocity = axisMappingItem.SetupVelocity;
            existing.PulseEquivalent = axisMappingItem.PulseEquivalent;
            existing.HomeMode = axisMappingItem.HomeMode;
            existing.ServoBinding = axisMappingItem.ServoBinding;
        }

        if (root.AxisMapping.AxisNames.Count <= axisMappingItem.AxisNo)
        {
            while (root.AxisMapping.AxisNames.Count <= axisMappingItem.AxisNo)
            {
                root.AxisMapping.AxisNames.Add($"Axis {root.AxisMapping.AxisNames.Count}");
            }
        }

        root.AxisMapping.AxisNames[axisMappingItem.AxisNo] = axisMappingItem.Name;

        var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(appSettingsPath, json, cancellationToken);
    }

    private async Task<AppSettingsRoot> LoadRootAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(appSettingsPath))
        {
            throw new FileNotFoundException($"App settings file not found: {appSettingsPath}");
        }

        var json = await File.ReadAllTextAsync(appSettingsPath, cancellationToken);
        return JsonSerializer.Deserialize<AppSettingsRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        }) ?? new AppSettingsRoot();
    }

    private sealed class AppSettingsRoot
    {
        public ZmcControllerConfig ZmcController { get; set; } = new();
        public AxisMappingOptions AxisMapping { get; set; } = new();
    }

    private sealed class ZmcControllerConfig
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int AxisCount { get; set; } = 32;
        public int PollingIntervalMs { get; set; } = 200;
    }
}
