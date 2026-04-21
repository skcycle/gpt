using MotionControl.Application.Interfaces;

namespace MotionControl.Presentation.ViewModels;

public sealed class AxisConsoleCoordinator(
    AxisMonitorViewModel axisMonitor,
    AxisDebugViewModel axisDebug,
    AxisParameterEditorViewModel axisParameterEditor)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        axisMonitor.SelectedAxisChanged += OnAxisMonitorSelectedAxisChanged;
        axisDebug.SelectedAxisChanged += OnAxisDebugSelectedAxisChanged;

        if (axisMonitor.SelectedAxis is not null)
        {
            await SyncSelectedAxisAsync(axisMonitor.SelectedAxis.AxisNo);
        }
    }

    private async void OnAxisMonitorSelectedAxisChanged(AxisViewModel? axis)
    {
        if (axis is null)
        {
            return;
        }

        await SyncSelectedAxisAsync(axis.AxisNo);
    }

    private async void OnAxisDebugSelectedAxisChanged(int axisNo)
    {
        await SyncSelectedAxisAsync(axisNo);
    }

    public async Task SyncSelectedAxisAsync(int axisNo)
    {
        axisDebug.SelectedAxisNo = axisNo;
        await axisParameterEditor.SyncAxisNoAsync(axisNo);
    }
}
