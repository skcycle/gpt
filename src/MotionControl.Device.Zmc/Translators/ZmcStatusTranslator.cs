using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Enums;

namespace MotionControl.Device.Zmc.Translators;

public sealed class ZmcStatusTranslator
{
    public AxisFeedback Translate(int axisNo, float dpos, float speed, int idle, int axisStatus)
    {
        var axisState = idle == 0 ? AxisState.Moving : AxisState.Standstill;
        var servoState = ServoState.On;
        var hasAlarm = axisStatus < 0;

        return new AxisFeedback(axisNo, dpos, speed, axisState, servoState, hasAlarm, false, false);
    }
}
