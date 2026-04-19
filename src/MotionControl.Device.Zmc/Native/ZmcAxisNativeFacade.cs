namespace MotionControl.Device.Zmc.Native;

public sealed class ZmcAxisNativeFacade
{
    public int EnableAxis(int axisNo)
    {
        // TODO: 对接 ZMC SDK 使能接口。
        return 0;
    }

    public int DisableAxis(int axisNo)
    {
        // TODO: 对接 ZMC SDK 去使能接口。
        return 0;
    }

    public int MoveAbsolute(int axisNo, double position, double velocity, double acceleration, double deceleration)
    {
        // TODO: 对接 ZMC SDK 绝对运动接口。
        return 0;
    }

    public int StopAxis(int axisNo)
    {
        // TODO: 对接 ZMC SDK 停止接口。
        return 0;
    }
}
