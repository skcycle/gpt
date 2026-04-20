using System.Linq;
using MotionControl.Device.Abstractions.Models;
using MotionControl.Domain.Enums;

namespace MotionControl.Device.Zmc.Translators;

public sealed class ZmcStatusTranslator
{
    private const int FollowingErrorAlarmBit = 1 << 1; // 2h
    private const int PlcCommErrorBit = 1 << 2; // 4h
    private const int DriveAlarmBit = 1 << 3; // 8h
    private const int PositiveHardLimitBit = 1 << 4; // 10h
    private const int NegativeHardLimitBit = 1 << 5; // 20h
    private const int FollowingErrorOutOfRangeBit = 1 << 8; // 100h
    private const int PositiveSoftLimitBit = 1 << 9; // 200h
    private const int NegativeSoftLimitBit = 1 << 10; // 400h
    private const int AxisWarningInputBit = 1 << 22; // 400000h
    private const int AxisInAlarmStateBit = 1 << 23; // 800000h

    public AxisFeedback Translate(int axisNo, float dpos, float mpos, float speed, int idle, int axisStatus, int homeStatus, int busEnableStatus)
    {
        var axisState = idle == 0 ? AxisState.Moving : AxisState.Standstill;
        var servoState = busEnableStatus != 0 ? ServoState.On : ServoState.Off;
        var hasAlarm = HasAny(axisStatus,
            FollowingErrorAlarmBit,
            PlcCommErrorBit,
            DriveAlarmBit,
            FollowingErrorOutOfRangeBit,
            AxisWarningInputBit,
            AxisInAlarmStateBit);
        var isHomed = homeStatus != 0;
        var positiveHardLimitTriggered = HasAny(axisStatus, PositiveHardLimitBit);
        var negativeHardLimitTriggered = HasAny(axisStatus, NegativeHardLimitBit);
        var positiveSoftLimitTriggered = HasAny(axisStatus, PositiveSoftLimitBit);
        var negativeSoftLimitTriggered = HasAny(axisStatus, NegativeSoftLimitBit);

        return new AxisFeedback(
            axisNo,
            dpos,
            mpos,
            speed,
            axisState,
            servoState,
            hasAlarm,
            isHomed,
            positiveHardLimitTriggered,
            negativeHardLimitTriggered,
            positiveSoftLimitTriggered,
            negativeSoftLimitTriggered);
    }

    private static bool HasAny(int axisStatus, params int[] masks)
        => masks.Any(mask => (axisStatus & mask) == mask);
}
