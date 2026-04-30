using System.Text.Json;
using MotionControl.Application.Interfaces;
using MotionControl.Domain.Entities;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class MagazineConfigAppService(string appSettingsPath) : IMagazineConfigAppService
{
    public async Task<IReadOnlyList<MagazineConfigItem>> LoadMagazinesAsync(CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        return root.MagazineMapping.Magazines.Select(Normalize).OrderBy(item => item.Name).ToList();
    }

    public async Task<MagazineConfigItem> AddMagazineAsync(CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        var index = root.MagazineMapping.Magazines.Count;
        var item = Normalize(new MagazineConfigItem
        {
            Name = $"Magazine {index + 1}",
            Description = string.Empty,
            XAxisNo = -1,
            YAxisNo = -1,
            ZAxisNo = -1,
            MaterialPresentInputAddress = -1,
            CurrentLayerHasMaterialInputAddress = -1,
            TrayKeyingInputAddress = -1,
            LayerCount = 1
        });
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
        root.MagazineMapping.Magazines = magazines.Select(Normalize).OrderBy(item => item.Name).ToList();
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

    private static MagazineConfigItem Normalize(MagazineConfigItem item)
    {
        item.Positions ??= new List<MagazinePositionConfigItem>();

        EnsureSystemPosition(item.Positions, MagazinePositionKinds.PickStart, "取料起始位");
        EnsureSystemPosition(item.Positions, MagazinePositionKinds.InspectStart, "检测起始位");

        foreach (var position in item.Positions)
        {
            if (string.IsNullOrWhiteSpace(position.Kind)) position.Kind = MagazinePositionKinds.Normal;
        }

        item.Positions = item.Positions
            .OrderBy(position => position.Kind switch
            {
                MagazinePositionKinds.PickStart => 0,
                MagazinePositionKinds.InspectStart => 1,
                _ => 2
            })
            .ThenBy(position => position.Name)
            .ToList();

        return item;
    }

    private static void EnsureSystemPosition(List<MagazinePositionConfigItem> positions, string kind, string name)
    {
        var existing = positions.FirstOrDefault(position => string.Equals(position.Kind, kind, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Name = name;
            existing.Kind = kind;
            return;
        }

        positions.Add(new MagazinePositionConfigItem
        {
            Name = name,
            Description = string.Empty,
            Kind = kind,
            X = 0,
            Y = 0,
            Z = 0
        });
    }

    private sealed class AppSettingsRoot
    {
        public MagazineMappingOptions MagazineMapping { get; set; } = new();
    }
}
