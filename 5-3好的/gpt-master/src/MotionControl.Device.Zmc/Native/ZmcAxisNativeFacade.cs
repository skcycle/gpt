using System.Text;

namespace MotionControl.Device.Zmc.Native;

/// <summary>
/// ZMC 控制器原生 API 的外观封装，提供线程安全的连接管理和轴操作。
/// 
/// 线程安全设计：
/// - _handle 标记为 volatile，单步操作（读状态、使能/失能等）通过无锁快照读取，
///   依赖 ZmcNativeApi 内部线程安全（官方 demo 已验证单实例多线程可用）。
/// - Connect/Disconnect 使用独立的 _connectLock，建联和断连期间不阻塞 IsConnected
///   或常规轴操作。
/// - MoveAbsolute / JogAxis / WriteAxisParameters 等多步复合操作使用 _multiStepLock
///   保证参数设置与执行指令的原子性。
/// - 所有 native 调用均在 try-catch 内，异常安全。
/// </summary>
public sealed class ZmcAxisNativeFacade
{
    // ZMC 在线探活命令。这里使用 ?SPEED(0)，因为它已经在现有项目链路中被验证可正常响应。
    // 不要随意替换成未验证的查询（例如 ?SYS_TIME），否则会出现"控制器已连接但被误判为断连"的问题。
    private const string ConnectionProbeCommand = "?SPEED(0)";

    private readonly object _connectLock = new();
    private readonly object _multiStepLock = new();

    /// <summary>
    /// volatile 保证跨线程可见性，单步操作通过快照读取避免锁竞争。
    /// </summary>
    private volatile IntPtr _handle = IntPtr.Zero;

    /// <summary>
    /// 获取当前连接状态（无锁，适合高频轮询）。
    /// </summary>
    public bool IsConnected => _handle != IntPtr.Zero;

    /// <summary>
    /// 连接到 ZMC 控制器。
    /// 使用独立的 _connectLock 串行化建联，不阻塞 IsConnected 读取和轴操作。
    /// 如果当前已有连接，会先断开旧连接再建立新连接。
    /// </summary>
    public int Connect(string ipAddress)
    {
        lock (_connectLock)
        {
            // 先标记断开，防止其他线程在 Close 期间使用旧 handle
            var oldHandle = _handle;
            _handle = IntPtr.Zero;

            if (oldHandle != IntPtr.Zero)
            {
                SafeClose(oldHandle);
            }

            IntPtr newHandle;
            int result;
            try
            {
                result = ZmcNativeApi.OpenEth(ipAddress, out newHandle);
            }
            catch
            {
                // 异常时确保 _handle 为 IntPtr.Zero（已在上面设过，再次确认）
                _handle = IntPtr.Zero;
                throw;
            }

            _handle = result == 0 ? newHandle : IntPtr.Zero;
            return result;
        }
    }

    /// <summary>
    /// 断开与 ZMC 控制器的连接。
    /// 重复调用、从未连接时调用均安全。
    /// </summary>
    public int Disconnect()
    {
        lock (_connectLock)
        {
            var handle = _handle;
            if (handle == IntPtr.Zero)
            {
                return 0;
            }

            // 先将 _handle 置零，阻止新操作获取该 handle
            _handle = IntPtr.Zero;

            // 已在执行中的单步操作持有的是 handle 快照，
            // ZmcNativeApi 内部线程安全，Close 不会影响进行中的调用（返回错误码）
            return SafeClose(handle);
        }
    }

    // ── 单步操作（无锁，volatile 快照 + native SDK 线程安全） ──

    public int EnableAxis(int axisNo)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectSetAxisEnable(handle, axisNo, 1); }
        catch { return -1; }
    }

    public int DisableAxis(int axisNo)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectSetAxisEnable(handle, axisNo, 0); }
        catch { return -1; }
    }

    public int GetAxisEnable(int axisNo, ref int value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetAxisEnable(handle, axisNo, ref value); }
        catch { return -1; }
    }

    public int HomeAxis(int axisNo)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ExecuteNativeCommand(handle, $"DATUM({axisNo})"); }
        catch { return -1; }
    }

    public int ClearDriveAlarm(int axisNo)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ExecuteNativeCommand(handle, $"ALMCLR({axisNo})"); }
        catch { return -1; }
    }

    public int ClearZmcAlarm(int axisNo)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ExecuteNativeCommand(handle, $"DATUM({axisNo},0)"); }
        catch { return -1; }
    }

    public int StopAxis(int axisNo)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectSingleCancel(handle, axisNo, 2); }
        catch { return -1; }
    }

    public int RapidStop(int mode)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ExecuteNativeCommand(handle, $"RAPIDSTOP({mode})"); }
        catch { return -1; }
    }

    public int GetAxisDpos(int axisNo, ref float value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetDpos(handle, axisNo, ref value); }
        catch { return -1; }
    }

    public int GetAxisMpos(int axisNo, ref float value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetMpos(handle, axisNo, ref value); }
        catch { return -1; }
    }

    public int GetAxisSpeed(int axisNo, ref float value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetSpeed(handle, axisNo, ref value); }
        catch { return -1; }
    }

    public int GetAxisIdle(int axisNo, ref int value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetIfIdle(handle, axisNo, ref value); }
        catch { return -1; }
    }

    public int GetAxisStatus(int axisNo, ref int value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetAxisStatus(handle, axisNo, ref value); }
        catch { return -1; }
    }

    public int GetAxisStatus2(int axisNo, int homeIn, ref int axisStatus, ref int idle, ref int homeStatus, ref int busEnableStatus)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetAxisStatus2(handle, axisNo, homeIn, ref axisStatus, ref idle, ref homeStatus, ref busEnableStatus); }
        catch { return -1; }
    }

    public int GetInput(int ioNo, ref uint value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetIn(handle, ioNo, ref value); }
        catch { return -1; }
    }

    public int GetOutput(int ioNo, ref uint value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectGetOp(handle, ioNo, ref value); }
        catch { return -1; }
    }

    public int SetOutput(int ioNo, int value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.DirectSetOp(handle, ioNo, value); }
        catch { return -1; }
    }

    /// <summary>
    /// 连接探活（无锁，适合轮询）。
    /// </summary>
    public int ProbeConnection()
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try
        {
            var buffer = new StringBuilder(64);
            return ZmcNativeApi.Execute(handle, ConnectionProbeCommand, buffer, 64);
        }
        catch { return -1; }
    }

    public string ReadAxisParameters(int axisNo)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return "Controller not connected";
        try
        {
            var buffer = new StringBuilder(512);
            var result = ZmcNativeApi.Execute(handle, $"?SPEED({axisNo})", buffer, 512);
            if (result != 0) return $"Read SPEED failed: {result}";
            return buffer.ToString();
        }
        catch (Exception ex) { return $"Read SPEED exception: {ex.Message}"; }
    }

    // ── 多步复合操作（_multiStepLock 保护原子性） ──

    /// <summary>
    /// 绝对定位运动。在一次锁内按序设置速度/加速度/减速度后执行定位，
    /// 保证不被其他复合操作打断。
    /// </summary>
    public int MoveAbsolute(int axisNo, double position, double velocity, double acceleration, double deceleration)
    {
        lock (_multiStepLock)
        {
            var handle = _handle;
            if (handle == IntPtr.Zero) return -1;

            try
            {
                var r1 = ExecuteNativeCommand(handle, $"SPEED({axisNo})={velocity}");
                if (r1 != 0) return r1;
                var r2 = ExecuteNativeCommand(handle, $"ACCEL({axisNo})={acceleration}");
                if (r2 != 0) return r2;
                var r3 = ExecuteNativeCommand(handle, $"DECEL({axisNo})={deceleration}");
                if (r3 != 0) return r3;
                return ZmcNativeApi.DirectSingleMoveAbs(handle, axisNo, (float)position);
            }
            catch { return -1; }
        }
    }

    /// <summary>
    /// Jog 运动。在一次锁内完成速度设置和方向指令，保证原子性。
    /// </summary>
    public int JogAxis(int axisNo, double velocity, bool positiveDirection)
    {
        lock (_multiStepLock)
        {
            var handle = _handle;
            if (handle == IntPtr.Zero) return -1;

            try
            {
                var setup = ExecuteNativeCommand(handle, $"SPEED({axisNo})={velocity}");
                if (setup != 0) return setup;
                return ZmcNativeApi.DirectSingleVmove(handle, axisNo, positiveDirection ? 1 : -1);
            }
            catch { return -1; }
        }
    }

    /// <summary>
    /// 写入轴参数。在一次锁内完成工作速度和寸动速度的写入。
    /// </summary>
    public int WriteAxisParameters(int axisNo, double workVelocity, double setupVelocity)
    {
        lock (_multiStepLock)
        {
            var handle = _handle;
            if (handle == IntPtr.Zero) return -1;

            try
            {
                var r1 = ExecuteNativeCommand(handle, $"SPEED({axisNo})={workVelocity}");
                if (r1 != 0) return r1;
                return ExecuteNativeCommand(handle, $"CREEP({axisNo})={setupVelocity}");
            }
            catch { return -1; }
        }
    }

    // ── EtherCAT 总线操作（单步，无锁 volatile 快照） ──

    /// <summary>获取总线扫描到的节点数量。</summary>
    public int GetBusNodeCount(ref int count)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.BusCmdGetNodeNum(handle, 0, ref count); }
        catch { return -1; }
    }

    /// <summary>读取总线节点信息（sel: 0-厂商ID 1-设备ID 2-版本 10-IN数 11-OUT数）。</summary>
    public int GetBusNodeInfo(uint node, uint sel, ref int value)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.BusCmdGetNodeInfo(handle, 0, node, sel, ref value); }
        catch { return -1; }
    }

    /// <summary>读取总线节点通讯状态。</summary>
    public int GetBusNodeStatus(uint node, ref uint status)
    {
        var handle = _handle;
        if (handle == IntPtr.Zero) return -1;
        try { return ZmcNativeApi.BusCmdGetNodeStatus(handle, 0, node, ref status); }
        catch { return -1; }
    }

    // ── 内部工具方法 ──

    private static int ExecuteNativeCommand(IntPtr handle, string command)
    {
        var buffer = new StringBuilder(256);
        return ZmcNativeApi.Execute(handle, command, buffer, 256);
    }

    /// <summary>
    /// 安全关闭 native handle，吞掉所有异常。
    /// </summary>
    private static int SafeClose(IntPtr handle)
    {
        try { return ZmcNativeApi.Close(handle); }
        catch { return -1; }
    }
}
