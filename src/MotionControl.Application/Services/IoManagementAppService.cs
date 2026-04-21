using MotionControl.Application.Interfaces;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

/// <summary>
/// IO 配置相关的用例编排服务。
/// 负责组合“配置读写”和“运行时同步”，
/// 让上层 ViewModel 不直接操作 Machine 或配置文件。
/// </summary>
public sealed class IoManagementAppService(
    IIoConfigAppService ioConfigAppService,
    IIoRuntimeSyncService ioRuntimeSyncService) : IIoManagementAppService
{
    public async Task<IoPointConfigItem> AddIoPointAsync(bool isOutput, CancellationToken cancellationToken = default)
    {
        var item = await ioConfigAppService.AddIoPointAsync(isOutput, cancellationToken);
        await ioRuntimeSyncService.ApplyAsync(item, cancellationToken);
        return item;
    }

    public async Task<bool> DeleteIoPointAsync(bool isOutput, int address, CancellationToken cancellationToken = default)
    {
        var removed = await ioConfigAppService.DeleteIoPointAsync(isOutput, address, cancellationToken);
        if (!removed)
        {
            return false;
        }

        await ioRuntimeSyncService.RemoveAsync(isOutput, address, cancellationToken);
        return true;
    }

    public async Task SaveIoPointsAsync(IEnumerable<IoPointConfigItem> ioPoints, CancellationToken cancellationToken = default)
    {
        await ioConfigAppService.SaveIoPointsAsync(ioPoints, cancellationToken);
        await ioRuntimeSyncService.ReloadAsync(ioPoints, cancellationToken);
    }

    public async Task<IReadOnlyList<IoPointConfigItem>> LoadIoPointsAsync(CancellationToken cancellationToken = default)
    {
        var items = await ioConfigAppService.LoadIoPointsAsync(cancellationToken);
        await ioRuntimeSyncService.ReloadAsync(items, cancellationToken);
        return items;
    }
}
