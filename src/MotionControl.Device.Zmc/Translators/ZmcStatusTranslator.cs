using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Enums;

namespace MotionControl.Device.Zmc.Translators;

public sealed class ZmcStatusTranslator
{
    public AxisFeedback Translate(int axisNo)
    {
        return new AxisFeedback(axisNo, 0, 0, AxisState.Standstill, ServoState.Off, false, false, false);
    }
}
