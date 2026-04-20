using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Enums;

namespace MotionControl.Device.Zmc.Translators;

public sealed class ZmcStatusTranslator
{
    public AxisFeedback Translate(int axisNo, float dpos, float speed, int idle, int axisStatus, int homeStatus, int busEnableStatus)
    {
        var axisState = idle == 0 ? AxisState.Moving : AxisState.Standstill;
        var servoState = busEnableStatus != 0 ? ServoState.On : ServoState.Off;
        var hasAlarm = axisStatus < 0;
        var isHomed = homeStatus != 0;

        return new AxisFeedback(axisNo, dpos, speed, axisState, servoState, hasAlarm, isHomed, false, false);
    }
}
