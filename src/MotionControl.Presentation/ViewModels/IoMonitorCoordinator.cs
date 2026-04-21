using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

public sealed class IoMonitorCoordinator(
    IoMonitorViewModel ioMonitor,
    RelayCommand deleteInputCommand,
    RelayCommand deleteOutputCommand)
{
    public void Initialize()
    {
        ioMonitor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IoMonitorViewModel.SelectedInput))
            {
                deleteInputCommand.RaiseCanExecuteChanged();
            }
            else if (args.PropertyName == nameof(IoMonitorViewModel.SelectedOutput))
            {
                deleteOutputCommand.RaiseCanExecuteChanged();
            }
        };
    }

    public void AfterLoadOrReload()
    {
        ioMonitor.SelectedInput = null;
        ioMonitor.SelectedOutput = null;
        ioMonitor.ReloadFromMachine();
        deleteInputCommand.RaiseCanExecuteChanged();
        deleteOutputCommand.RaiseCanExecuteChanged();
    }

    public void AfterDelete(bool isOutput, int address)
    {
        ioMonitor.RemoveIoPoint(isOutput, address);
        if (isOutput)
        {
            deleteOutputCommand.RaiseCanExecuteChanged();
        }
        else
        {
            deleteInputCommand.RaiseCanExecuteChanged();
        }
    }
}
