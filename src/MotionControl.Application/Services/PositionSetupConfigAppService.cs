using System.Text.Json;
using MotionControl.Application.Interfaces;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class PositionSetupConfigAppService(string appSettingsPath) : IPositionSetupConfigAppService
{
    public async Task<IReadOnlyList<PositionSetupConfigItem>> LoadPositionsAsync(CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        return root.PositionSetupMapping.Positions.OrderBy(item => item.Name).ToList();
    }

    public async Task<PositionSetupConfigItem> AddPositionAsync(CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        var index = root.PositionSetupMapping.Positions.Count + 1;
        var item = new PositionSetupConfigItem { Name = $"Position {index}" };
        root.PositionSetupMapping.Positions.Add(item);
        await SaveRootAsync(root, cancellationToken);
        return item;
    }

    public async Task SavePositionsAsync(IEnumerable<PositionSetupConfigItem> positions, CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        root.PositionSetupMapping.Positions = positions.OrderBy(item => item.Name).ToList();
        await SaveRootAsync(root, cancellationToken);
    }

    private async Task<AppSettingsRoot> LoadRootAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(appSettingsPath)) return new AppSettingsRoot();
        var json = await File.ReadAllTextAsync(appSettingsPath, cancellationToken);
        return JsonSerializer.Deserialize<AppSettingsRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AppSettingsRoot();
    }

    private async Task SaveRootAsync(AppSettingsRoot root, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(appSettingsPath, json, cancellationToken);
    }

    private sealed class AppSettingsRoot
    {
        public IoMappingOptions IoMapping { get; set; } = new();
        public CylinderMappingOptions CylinderMapping { get; set; } = new();
        public WorkHeadMappingOptions WorkHeadMapping { get; set; } = new();
        public PositionSetupMappingOptions PositionSetupMapping { get; set; } = new();
    }
}
