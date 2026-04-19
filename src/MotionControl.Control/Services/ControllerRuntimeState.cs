using MotionControl.Device.Abstractions.Models;

namespace MotionControl.Control.Services;

public sealed class ControllerRuntimeState
{
    public EtherCatControllerStatus? LastControllerStatus { get; private set; }

    public void Update(EtherCatControllerStatus controllerStatus)
    {
        LastControllerStatus = controllerStatus;
    }
}
