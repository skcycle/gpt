namespace MotionControl.Infrastructure.Logging;

public sealed class StructuredLogService
{
    public void Info(string message)
    {
        // TODO: 接入正式日志框架，如 Serilog / NLog。
        Console.WriteLine($"[INFO] {DateTime.Now:O} {message}");
    }
}
