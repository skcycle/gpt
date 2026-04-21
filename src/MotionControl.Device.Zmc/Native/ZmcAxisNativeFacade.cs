using System.Text;

namespace MotionControl.Device.Zmc.Native;

public sealed class ZmcAxisNativeFacade
{
    // ZMC 在线探活命令。这里使用 ?SPEED(0)，因为它已经在现有项目链路中被验证可正常响应。
    // 不要随意替换成未验证的查询（例如 ?SYS_TIME），否则会出现“控制器已连接但被误判为断连”的问题。
    private const string ConnectionProbeCommand = "?SPEED(0)";

    private IntPtr _handle = IntPtr.Zero;

    public bool IsConnected => _handle != IntPtr.Zero;

    public int Connect(string ipAddress)
    {
        var result = ZmcNativeApi.OpenEth(ipAddress, out var handle);
        if (result == 0)
        {
            _handle = handle;
        }

        return result;
    }

    public int Disconnect()
    {
        if (_handle == IntPtr.Zero)
        {
            return 0;
        }

        var result = ZmcNativeApi.Close(_handle);
        if (result == 0)
        {
            _handle = IntPtr.Zero;
        }

        return result;
    }

    public int EnableAxis(int axisNo)
    {
        if (_handle == IntPtr.Zero) return -1;
        return ZmcNativeApi.DirectSetAxisEnable(_handle, axisNo, 1);
    }

    public int DisableAxis(int axisNo)
    {
        if (_handle == IntPtr.Zero) return -1;
        return ZmcNativeApi.DirectSetAxisEnable(_handle, axisNo, 0);
    }

    public int GetAxisEnable(int axisNo, ref int value)
    {
        if (_handle == IntPtr.Zero) return -1;
        return ZmcNativeApi.DirectGetAxisEnable(_handle, axisNo, ref value);
    }

    public int HomeAxis(int axisNo)
    {
        return ExecuteCommand($"DATUM({axisNo})");
    }

    public int ClearDriveAlarm(int axisNo)
    {
        return ExecuteCommand($"ALMCLR({axisNo})");
    }

    public int ClearZmcAlarm(int axisNo)
    {
        return ExecuteCommand($"DATUM({axisNo},0)");
    }

    public int MoveAbsolute(int axisNo, double position, double velocity, double acceleration, double deceleration)
    {
        var setup1 = ExecuteCommand($"SPEED({axisNo})={velocity}");
        if (setup1 != 0) return setup1;
        var setup2 = ExecuteCommand($"ACCEL({axisNo})={acceleration}");
        if (setup2 != 0) return setup2;
        var setup3 = ExecuteCommand($"DECEL({axisNo})={deceleration}");
        if (setup3 != 0) return setup3;
        return ZmcNativeApi.DirectSingleMoveAbs(_handle, axisNo, (float)position);
    }

    public int JogAxis(int axisNo, double velocity, bool positiveDirection)
    {
        var setup = ExecuteCommand($"SPEED({axisNo})={velocity}");
        if (setup != 0)
        {
            return setup;
        }

        return ZmcNativeApi.DirectSingleVmove(_handle, axisNo, positiveDirection ? 1 : -1);
    }

    public int StopAxis(int axisNo)
    {
        return ZmcNativeApi.DirectSingleCancel(_handle, axisNo, 2);
    }

    public int RapidStop(int mode)
    {
        return ExecuteCommand($"RAPIDSTOP({mode})");
    }

    public int GetAxisDpos(int axisNo, ref float value) => ZmcNativeApi.DirectGetDpos(_handle, axisNo, ref value);
    public int GetAxisMpos(int axisNo, ref float value) => ZmcNativeApi.DirectGetMpos(_handle, axisNo, ref value);
    public int GetAxisSpeed(int axisNo, ref float value) => ZmcNativeApi.DirectGetSpeed(_handle, axisNo, ref value);
    public int GetAxisIdle(int axisNo, ref int value) => ZmcNativeApi.DirectGetIfIdle(_handle, axisNo, ref value);
    public int GetAxisStatus(int axisNo, ref int value) => ZmcNativeApi.DirectGetAxisStatus(_handle, axisNo, ref value);
    public int GetAxisStatus2(int axisNo, int homeIn, ref int axisStatus, ref int idle, ref int homeStatus, ref int busEnableStatus)
        => ZmcNativeApi.DirectGetAxisStatus2(_handle, axisNo, homeIn, ref axisStatus, ref idle, ref homeStatus, ref busEnableStatus);
    public int GetInput(int ioNo, ref uint value) => ZmcNativeApi.DirectGetIn(_handle, ioNo, ref value);
    public int GetOutput(int ioNo, ref uint value) => ZmcNativeApi.DirectGetOp(_handle, ioNo, ref value);

    public int SetOutput(int ioNo, int value) => ZmcNativeApi.DirectSetOp(_handle, ioNo, value);

    public int ProbeConnection()
    {
        if (_handle == IntPtr.Zero)
        {
            return -1;
        }

        var buffer = new StringBuilder(64);
        return ZmcNativeApi.Execute(_handle, ConnectionProbeCommand, buffer, 64);
    }

    public string ReadAxisParameters(int axisNo)
    {
        if (_handle == IntPtr.Zero)
        {
            return "Controller not connected";
        }

        var buffer = new StringBuilder(512);
        var result = ZmcNativeApi.Execute(_handle, $"?SPEED({axisNo})", buffer, 512);
        if (result != 0)
        {
            return $"Read SPEED failed: {result}";
        }

        return buffer.ToString();
    }

    public int WriteAxisParameters(int axisNo, double workVelocity, double setupVelocity, double pulseEquivalent)
    {
        var result = ExecuteCommand($"SPEED({axisNo})={workVelocity}");
        if (result != 0) return result;
        result = ExecuteCommand($"CREEP({axisNo})={setupVelocity}");
        if (result != 0) return result;
        return ExecuteCommand($"UNITS({axisNo})={pulseEquivalent}");
    }

    private int ExecuteCommand(string command)
    {
        if (_handle == IntPtr.Zero)
        {
            return -1;
        }

        var buffer = new StringBuilder(256);
        return ZmcNativeApi.Execute(_handle, command, buffer, 256);
    }
}
