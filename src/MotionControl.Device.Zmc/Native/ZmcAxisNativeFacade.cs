using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MotionControl.Device.Zmc.Native;

/// <summary>
/// ZMC 控制器原生 API 的外观封装，提供线程安全的连接管理和轴操作。
///
/// <para><b>线程安全模型</b></para>
/// <list type="bullet">
/// <item><b>ReaderWriterLockSlim</b> 保护 _handle 生命周期：
///   所有操作（读/写轴参数、IO、探活）获取读锁（可并发）；
///   Connect/Disconnect 获取写锁（排他，等待所有读锁释放后执行）。</item>
/// <item><b>_connectLock</b> 串行化 Connect 调用，防止多线程同时建联。</item>
/// <item><b>_multiStepLock</b> 串行化 MoveAbsolute/JogAxis/WriteAxisParameters
///   等复合操作，防止参数设置与执行指令被交错。</item>
/// <item>Disconnect 在写锁内执行：先置 _handle=IntPtr.Zero 阻断新操作，
///   等待所有进行中的读锁释放，然后安全 Close。不存在 handle 竞态。</item>
/// </list>
///
/// <para><b>多轴并发</b></para>
/// 单步操作（读状态、IO、使能/失能）可真正并发 — SDK 已证明
/// 单实例多线程用；复合操作（MoveAbsolute 等）因 _multiStepLock 会串行化
/// 所有轴的运动指令序列。这是保守安全策略，与控制器单命令队列的物理现实一致。
/// </para>
/// <para><b>⚠ 锁顺序规则（新增方法必须遵守）</b></para>
/// 1. 单步操作：只拿 _handleLock 读锁（无需 _multiStepLock）
/// 2. 复合操作：先拿 _handleLock 读锁，再拿 _multiStepLock，释放时反序
/// 3. Connect/Disconnect：拿 _handleLock 写锁（排他，等所有读写锁释放）
/// <b>禁止</b>：先拿 _multiStepLock 再拿 _handleLock（会导致与 Connect 死锁）
/// </summary>
public sealed class ZmcAxisNativeFacade
{
    private const string ConnectionProbeCommand = "?SPEED(0)";

    // 连接串行化：防止多线程同时建联
    private readonly object _connectLock = new();

    // handle 生命周期保护：读锁=操作进行中，写锁=连接/断开
    private readonly ReaderWriterLockSlim _handleLock = new();

    // 复合操作串行化：保证 SPEED→ACCEL→DECEL→Move 原子性
    private readonly object _multiStepLock = new();

    private readonly ILogger<ZmcAxisNativeFacade> _logger;
    private IntPtr _handle = IntPtr.Zero;

    public ZmcAxisNativeFacade() : this(NullLogger<ZmcAxisNativeFacade>.Instance) { }

    public ZmcAxisNativeFacade(ILogger<ZmcAxisNativeFacade> logger)
    {
        _logger = logger ?? NullLogger<ZmcAxisNativeFacade>.Instance;
    }

    // ── 连接状态 ──

    /// <summary>获取当前连接状态（读锁，可与操作并发）。</summary>
    public bool IsConnected
    {
        get
        {
            _handleLock.EnterReadLock();
            try { return _handle != IntPtr.Zero; }
            finally { _handleLock.ExitReadLock(); }
        }
    }

    // ── 连接管理（写锁） ──

    public int Connect(string ipAddress)
    {
        lock (_connectLock)
        {
            _handleLock.EnterWriteLock();
            try
            {
                if (_handle != IntPtr.Zero)
                {
                    _logger.LogWarning("ZMC reconnecting: closing existing handle before new connect to {IpAddress}", ipAddress);
                    SafeClose(_handle);
                    _handle = IntPtr.Zero;
                }

                _logger.LogDebug("ZMC OpenEth connecting to {IpAddress}", ipAddress);
                var result = ZmcNativeApi.OpenEth(ipAddress, out var newHandle);
                if (result != 0)
                {
                    _logger.LogError("ZMC OpenEth failed: error={Result} ip={IpAddress}", result, ipAddress);
                    _handle = IntPtr.Zero;
                    return result;
                }

                _handle = newHandle;
                _logger.LogInformation("ZMC connected to {IpAddress}", ipAddress);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZMC OpenEth exception for {IpAddress}", ipAddress);
                _handle = IntPtr.Zero;
                return -1;
            }
            finally
            {
                _handleLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// 断开连接。写锁保证 Close 时没有进行中的操作。
    /// </summary>
    public int Disconnect()
    {
        _handleLock.EnterWriteLock();
        try
        {
            if (_handle == IntPtr.Zero) return 0;

            _logger.LogDebug("ZMC disconnecting");
            var handle = _handle;
            _handle = IntPtr.Zero; // 先标记断开
            return SafeClose(handle);
        }
        finally
        {
            _handleLock.ExitWriteLock();
        }
    }

    // ── 单步操作（读锁，可并发） ──

    public int EnableAxis(int axisNo)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("EnableAxis", -1);
            return SafeCall(() => ZmcNativeApi.DirectSetAxisEnable(_handle, axisNo, 1), "EnableAxis", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int DisableAxis(int axisNo)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("DisableAxis", -1);
            return SafeCall(() => ZmcNativeApi.DirectSetAxisEnable(_handle, axisNo, 0), "DisableAxis", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetAxisEnable(int axisNo, ref int value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetAxisEnable", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetAxisEnable(_handle, axisNo, ref value), "GetAxisEnable", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int HomeAxis(int axisNo)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("HomeAxis", -1);
            return SafeCall(() => ExecuteNativeCommand(_handle, $"DATUM({axisNo})"), "HomeAxis", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int ClearDriveAlarm(int axisNo)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("ClearDriveAlarm", -1);
            return SafeCall(() => ExecuteNativeCommand(_handle, $"ALMCLR({axisNo})"), "ClearDriveAlarm", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int ClearZmcAlarm(int axisNo)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("ClearZmcAlarm", -1);
            return SafeCall(() => ExecuteNativeCommand(_handle, $"DATUM({axisNo},0)"), "ClearZmcAlarm", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int StopAxis(int axisNo)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("StopAxis", -1);
            return SafeCall(() => ZmcNativeApi.DirectSingleCancel(_handle, axisNo, 2), "StopAxis", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int RapidStop(int mode)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("RapidStop", -1);
            return SafeCall(() => ExecuteNativeCommand(_handle, $"RAPIDSTOP({mode})"), "RapidStop");
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetAxisDpos(int axisNo, ref float value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetDpos", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetDpos(_handle, axisNo, ref value), "GetDpos", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetAxisMpos(int axisNo, ref float value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetMpos", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetMpos(_handle, axisNo, ref value), "GetMpos", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetAxisSpeed(int axisNo, ref float value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetSpeed", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetSpeed(_handle, axisNo, ref value), "GetSpeed", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetAxisIdle(int axisNo, ref int value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetIdle", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetIfIdle(_handle, axisNo, ref value), "GetIdle", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetAxisStatus(int axisNo, ref int value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetAxisStatus", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetAxisStatus(_handle, axisNo, ref value), "GetAxisStatus", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetAxisStatus2(int axisNo, int homeIn, ref int axisStatus, ref int idle, ref int homeStatus, ref int busEnableStatus)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetAxisStatus2", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetAxisStatus2(_handle, axisNo, homeIn, ref axisStatus, ref idle, ref homeStatus, ref busEnableStatus), "GetAxisStatus2", axisNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetInput(int ioNo, ref uint value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetInput", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetIn(_handle, ioNo, ref value), "GetInput", ioNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetOutput(int ioNo, ref uint value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("GetOutput", -1);
            return SafeCall(() => ZmcNativeApi.DirectGetOp(_handle, ioNo, ref value), "GetOutput", ioNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int SetOutput(int ioNo, int value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("SetOutput", -1);
            return SafeCall(() => ZmcNativeApi.DirectSetOp(_handle, ioNo, value), "SetOutput", ioNo);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int ProbeConnection()
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return -1;
            return SafeCall(() =>
            {
                var buffer = new StringBuilder(64);
                return ZmcNativeApi.Execute(_handle, ConnectionProbeCommand, buffer, 64);
            }, "ProbeConnection");
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public string ReadAxisParameters(int axisNo)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return "Controller not connected";
            try
            {
                var buffer = new StringBuilder(512);
                var result = ZmcNativeApi.Execute(_handle, $"?SPEED({axisNo})", buffer, 512);
                if (result != 0)
                {
                    _logger.LogWarning("ReadAxisParameters(?SPEED) failed: axis={AxisNo} result={Result}", axisNo, result);
                    return $"Read SPEED failed: {result}";
                }
                return buffer.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadAxisParameters(?SPEED) exception: axis={AxisNo}", axisNo);
                return $"Read SPEED exception: {ex.Message}";
            }
        }
        finally { _handleLock.ExitReadLock(); }
    }

    // ── EtherCAT 总线操作（读锁） ──

    public int GetBusNodeCount(ref int count)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("BusNodeCount", -1);
            return SafeCall(() => ZmcNativeApi.BusCmdGetNodeNum(_handle, 0, ref count), "BusNodeCount");
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetBusNodeInfo(uint node, uint sel, ref int value)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("BusNodeInfo", -1);
            return SafeCall(() => ZmcNativeApi.BusCmdGetNodeInfo(_handle, 0, node, sel, ref value), "BusNodeInfo", (int)node);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int GetBusNodeStatus(uint node, ref uint status)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("BusNodeStatus", -1);
            return SafeCall(() => ZmcNativeApi.BusCmdGetNodeStatus(_handle, 0, node, ref status), "BusNodeStatus", (int)node);
        }
        finally { _handleLock.ExitReadLock(); }
    }

    // ── 多步复合操作（读锁 + _multiStepLock） ──

    /// <summary>
    /// 绝对定位运动。_multiStepLock 保证参数设置与执行指令的原子性。
    /// _handleLock 读锁保证操作期间连接不被关闭。
    /// </summary>
    public int MoveAbsolute(int axisNo, double position, double velocity, double acceleration, double deceleration)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("MoveAbsolute", -1);

            lock (_multiStepLock)
            {
                try
                {
                    var r1 = ExecuteNativeCommand(_handle, $"SPEED({axisNo})={velocity}");
                    if (r1 != 0) return LogAndReturn("MoveAbsolute/SPEED", r1);
                    var r2 = ExecuteNativeCommand(_handle, $"ACCEL({axisNo})={acceleration}");
                    if (r2 != 0) return LogAndReturn("MoveAbsolute/ACCEL", r2);
                    var r3 = ExecuteNativeCommand(_handle, $"DECEL({axisNo})={deceleration}");
                    if (r3 != 0) return LogAndReturn("MoveAbsolute/DECEL", r3);
                    var r4 = ZmcNativeApi.DirectSingleMoveAbs(_handle, axisNo, (float)position);
                    if (r4 != 0) return LogAndReturn("MoveAbsolute/MoveAbs", r4);
                    return 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MoveAbsolute exception: axis={AxisNo}", axisNo);
                    return -1;
                }
            }
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int JogAxis(int axisNo, double velocity, bool positiveDirection)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("JogAxis", -1);

            lock (_multiStepLock)
            {
                try
                {
                    var setup = ExecuteNativeCommand(_handle, $"SPEED({axisNo})={velocity}");
                    if (setup != 0) return LogAndReturn("JogAxis/SPEED", setup);
                    var r = ZmcNativeApi.DirectSingleVmove(_handle, axisNo, positiveDirection ? 1 : -1);
                    if (r != 0) return LogAndReturn("JogAxis/Vmove", r);
                    return 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JogAxis exception: axis={AxisNo}", axisNo);
                    return -1;
                }
            }
        }
        finally { _handleLock.ExitReadLock(); }
    }

    public int WriteAxisParameters(int axisNo, double workVelocity, double setupVelocity)
    {
        _handleLock.EnterReadLock();
        try
        {
            if (_handle == IntPtr.Zero) return LogAndReturn("WriteAxisParams", -1);

            lock (_multiStepLock)
            {
                try
                {
                    var r1 = ExecuteNativeCommand(_handle, $"SPEED({axisNo})={workVelocity}");
                    if (r1 != 0) return LogAndReturn("WriteAxisParams/SPEED", r1);
                    var r2 = ExecuteNativeCommand(_handle, $"CREEP({axisNo})={setupVelocity}");
                    if (r2 != 0) return LogAndReturn("WriteAxisParams/CREEP", r2);
                    return 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WriteAxisParameters exception: axis={AxisNo}", axisNo);
                    return -1;
                }
            }
        }
        finally { _handleLock.ExitReadLock(); }
    }

    // ── 内部方法 ──

    private static int ExecuteNativeCommand(IntPtr handle, string command)
    {
        var buffer = new StringBuilder(256);
        return ZmcNativeApi.Execute(handle, command, buffer, 256);
    }

    private int SafeCall(Func<int> nativeCall, string operation, int? axisOrIo = null)
    {
        try
        {
            var result = nativeCall();
            if (result != 0)
                _logger.LogWarning("ZMC {Operation} failed: result={Result} id={Id}", operation, result, axisOrIo);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZMC {Operation} exception: id={Id}", operation, axisOrIo);
            return -1;
        }
    }

    private int LogAndReturn(string operation, int result)
    {
        if (result != 0)
            _logger.LogWarning("ZMC {Operation} returned {Result}", operation, result);
        return result;
    }

    private int SafeClose(IntPtr handle)
    {
        try { return ZmcNativeApi.Close(handle); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZMC Close exception");
            return -1;
        }
    }
}
