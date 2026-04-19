using System.Runtime.InteropServices;
using System.Text;

namespace MotionControl.Device.Zmc.Native;

public static class ZmcNativeApi
{
    [DllImport("zauxdll.dll", EntryPoint = "ZAux_OpenEth", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int OpenEth(string ipaddr, out IntPtr handle);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int Close(IntPtr handle);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Execute", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int Execute(IntPtr handle, string command, StringBuilder response, uint responseLength);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetAtype", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectSetAtype(IntPtr handle, int axisNo, int value);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_Single_MoveAbs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectSingleMoveAbs(IntPtr handle, int axisNo, float distance);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_Single_Vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectSingleVmove(IntPtr handle, int axisNo, int direction);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_Single_Cancel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectSingleCancel(IntPtr handle, int axisNo, int mode);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetDpos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectGetDpos(IntPtr handle, int axisNo, ref float value);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetMpos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectGetMpos(IntPtr handle, int axisNo, ref float value);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetSpeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectGetSpeed(IntPtr handle, int axisNo, ref float value);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetIfIdle", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectGetIfIdle(IntPtr handle, int axisNo, ref int value);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetAxisStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectGetAxisStatus(IntPtr handle, int axisNo, ref int value);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetIn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectGetIn(IntPtr handle, int ioNo, ref uint value);

    [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetOp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int DirectGetOp(IntPtr handle, int ioNo, ref uint value);
}
