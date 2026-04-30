using System.Text.Json;
using MotionControl.Application.Interfaces;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class MagazineConfigAppService(string appSettingsPath) : IMagazineConfigAppService
{
    public async Task<IReadOnlyList<MagazineConfigItem>> LoadMagazinesAsync(CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        return root.MagazineMapping.Magazines.OrderBy(item => item.Name).ToList();
    }

    public async Task<MagazineConfigItem> AddMagazineAsync(CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        var index = root.MagazineMapping.Magazines.Count;
        var item = new MagazineConfigItem
        {
            Name = $"Magazine {index + 1}",
            Description = string.Empty,
            XAxisNo = -1,
            YAxisNo = -1,
            ZAxisNo = -1,
            VacuumOutputAddress = -1,
            BlowOutputAddress = -1,
            MaterialPresentInputAddress = -1,
            CurrentLayerHasMaterialInputAddress = -1,
            TrayKeyingInputAddress = -1,
            LayerCount = 1,
            ActionTimeoutMs = 3000
        };
        root.MagazineMapping.Magazines.Add(item);
        await SaveRootAsync(root, cancellationToken);
        return item;
    }

    public async Task<bool> DeleteMagazineAsync(string name, CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        var removed = root.MagazineMapping.Magazines.RemoveAll(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed) return false;
        await SaveRootAsync(root, cancellationToken);
        return true;
    }

    public async Task SaveMagazinesAsync(IEnumerable<MagazineConfigItem> magazines, CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        root.MagazineMapping.Magazines = magazines.OrderBy(item => item.Name).ToList();
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
        public MagazineMappingOptions MagazineMapping { get; set; } = new();
    }
}
