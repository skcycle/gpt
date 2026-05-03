using System.Text;

namespace MotionControl.Device.Zmc.Native;

public sealed class ZmcAxisNativeFacade
{
    // ZMC 在线探活命令。这里使用 ?SPEED(0)，因为它已经在现有项目链路中被验证可正常响应。
    // 不要随意替换成未验证的查询（例如 ?SYS_TIME），否则会出现"控制器已连接但被误判为断连"的问题。
    private const string ConnectionProbeCommand = "?SPEED(0)";

    private readonly object _syncLock = new();
    private IntPtr _handle = IntPtr.Zero;

    /// <summary>
    /// 获取当前连接状态（线程安全）。
    /// </summary>
    public bool IsConnected
    {
        get
        {
            lock (_syncLock)
            {
                return _handle != IntPtr.Zero;
            }
        }
    }

    /// <summary>
    /// 连接到 ZMC 控制器（线程安全）。
    /// 如果当前已有连接，会先断开旧连接再建立新连接，防止 handle 泄漏。
    /// </summary>
    public int Connect(string ipAddress)
    {
        lock (_syncLock)
        {
            // 防止多线程重复连接导致 handle 泄漏
            if (_handle != IntPtr.Zero)
            {
                ZmcNativeApi.Close(_handle);
                _handle = IntPtr.Zero;
            }

            var result = ZmcNativeApi.OpenEth(ipAddress, out var handle);
            if (result == 0)
            {
                _handle = handle;
            }

            return result;
        }
    }

    /// <summary>
    /// 断开与 ZMC 控制器的连接（线程安全）。
    /// 重复调用不会产生副作用。
    /// </summary>
    public int Disconnect()
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero)
            {
                return 0;
            }

            var result = ZmcNativeApi.Close(_handle);
            _handle = IntPtr.Zero;
            return result;
        }
    }

    public int EnableAxis(int axisNo)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectSetAxisEnable(_handle, axisNo, 1);
        }
    }

    public int DisableAxis(int axisNo)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectSetAxisEnable(_handle, axisNo, 0);
        }
    }

    public int GetAxisEnable(int axisNo, ref int value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetAxisEnable(_handle, axisNo, ref value);
        }
    }

    public int HomeAxis(int axisNo)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            var buffer = new StringBuilder(256);
            return ZmcNativeApi.Execute(_handle, $"DATUM({axisNo})", buffer, 256);
        }
    }

    public int ClearDriveAlarm(int axisNo)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            var buffer = new StringBuilder(256);
            return ZmcNativeApi.Execute(_handle, $"ALMCLR({axisNo})", buffer, 256);
        }
    }

    public int ClearZmcAlarm(int axisNo)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            var buffer = new StringBuilder(256);
            return ZmcNativeApi.Execute(_handle, $"DATUM({axisNo},0)", buffer, 256);
        }
    }

    /// <summary>
    /// 绝对定位运动（线程安全）。
    /// 在一次锁内依次设置速度、加速度、减速度，然后执行定位，
    /// 防止中途被其他线程的 Disconnect 或参数修改打断。
    /// </summary>
    public int MoveAbsolute(int axisNo, double position, double velocity, double acceleration, double deceleration)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;

            var setup1 = ExecuteCommandLocked($"SPEED({axisNo})={velocity}");
            if (setup1 != 0) return setup1;
            var setup2 = ExecuteCommandLocked($"ACCEL({axisNo})={acceleration}");
            if (setup2 != 0) return setup2;
            var setup3 = ExecuteCommandLocked($"DECEL({axisNo})={deceleration}");
            if (setup3 != 0) return setup3;
            return ZmcNativeApi.DirectSingleMoveAbs(_handle, axisNo, (float)position);
        }
    }

    /// <summary>
    /// Jog 运动（线程安全）。
    /// 在一次锁内设置速度并启劯 Jog，保证参数设置与运动指令的原子性。
    /// </summary>
    public int JogAxis(int axisNo, double velocity, bool positiveDirection)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;

            var setup = ExecuteCommandLocked($"SPEED({axisNo})={velocity}");
            if (setup != 0)
            {
                return setup;
            }

            return ZmcNativeApi.DirectSingleVmove(_handle, axisNo, positiveDirection ? 1 : -1);
        }
    }

    public int StopAxis(int axisNo)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectSingleCancel(_handle, axisNo, 2);
        }
    }

    public int RapidStop(int mode)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            var buffer = new StringBuilder(256);
            return ZmcNativeApi.Execute(_handle, $"RAPIDSTOP({mode})", buffer, 256);
        }
    }

    public int GetAxisDpos(int axisNo, ref float value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetDpos(_handle, axisNo, ref value);
        }
    }

    public int GetAxisMpos(int axisNo, ref float value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetMpos(_handle, axisNo, ref value);
        }
    }

    public int GetAxisSpeed(int axisNo, ref float value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetSpeed(_handle, axisNo, ref value);
        }
    }

    public int GetAxisIdle(int axisNo, ref int value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetIfIdle(_handle, axisNo, ref value);
        }
    }

    public int GetAxisStatus(int axisNo, ref int value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetAxisStatus(_handle, axisNo, ref value);
        }
    }

    public int GetAxisStatus2(int axisNo, int homeIn, ref int axisStatus, ref int idle, ref int homeStatus, ref int busEnableStatus)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetAxisStatus2(_handle, axisNo, homeIn, ref axisStatus, ref idle, ref homeStatus, ref busEnableStatus);
        }
    }

    public int GetInput(int ioNo, ref uint value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetIn(_handle, ioNo, ref value);
        }
    }

    public int GetOutput(int ioNo, ref uint value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectGetOp(_handle, ioNo, ref value);
        }
    }

    public int SetOutput(int ioNo, int value)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;
            return ZmcNativeApi.DirectSetOp(_handle, ioNo, value);
        }
    }

    public int ProbeConnection()
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero)
            {
                return -1;
            }

            var buffer = new StringBuilder(64);
            return ZmcNativeApi.Execute(_handle, ConnectionProbeCommand, buffer, 64);
        }
    }

    public string ReadAxisParameters(int axisNo)
    {
        lock (_syncLock)
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
    }

    /// <summary>
    /// 写入轴参数（线程安全）。
    /// 在一次锁内完成工作速度和寸动速度的写入，避免写一半被中断。
    /// </summary>
    public int WriteAxisParameters(int axisNo, double workVelocity, double setupVelocity)
    {
        lock (_syncLock)
        {
            if (_handle == IntPtr.Zero) return -1;

            var result = ExecuteCommandLocked($"SPEED({axisNo})={workVelocity}");
            if (result != 0) return result;
            return ExecuteCommandLocked($"CREEP({axisNo})={setupVelocity}");
        }
    }

    /// <summary>
    /// 在调用方已持有 _syncLock 锁的情况下执行 ZMC 命令（内部使用，不加锁）。
    /// </summary>
    private int ExecuteCommandLocked(string command)
    {
        var buffer = new StringBuilder(256);
        return ZmcNativeApi.Execute(_handle, command, buffer, 256);
    }
}
