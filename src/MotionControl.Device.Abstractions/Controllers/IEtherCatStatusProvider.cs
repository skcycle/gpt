using MotionControl.Device.Abstractions.Models;

namespace MotionControl.Device.Abstractions.Controllers;

public interface IEtherCatStatusProvider
{
    EtherCatControllerStatus CreateStatus(bool isConnected, int axisCount);
}
