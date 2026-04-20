using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Entities;

public sealed class Machine
{
    private readonly List<Alarm> _alarms;

    public Machine(
        IReadOnlyCollection<Axis> axes,
        IReadOnlyCollection<AxisGroup> groups,
        IReadOnlyCollection<IoPoint>? ioPoints = null,
        IReadOnlyCollection<Alarm>? alarms = null)
    {
        Axes = axes;
        Groups = groups;
        IoPoints = ioPoints ?? Array.Empty<IoPoint>();
        _alarms = alarms?.ToList() ?? new List<Alarm>();
    }

    public IReadOnlyCollection<Axis> Axes { get; }
    public IReadOnlyCollection<AxisGroup> Groups { get; }
    public IReadOnlyCollection<IoPoint> IoPoints { get; }
    public IReadOnlyCollection<Alarm> Alarms => _alarms;
    public SystemState CurrentState { get; private set; } = SystemState.Initializing;
    public bool IsConnected { get; private set; }

    public void SetSystemState(SystemState state) => CurrentState = state;

    public void SetConnected(bool connected) => IsConnected = connected;

    public bool UpsertAlarm(string code, string message, string source = "System", string category = "General", string severity = "Error")
    {
        var existing = _alarms.FirstOrDefault(alarm => alarm.Code == code && alarm.IsActive);
        if (existing is not null)
        {
            return false;
        }

        _alarms.RemoveAll(alarm => alarm.Code == code && !alarm.IsActive);
        _alarms.Add(new Alarm(code, message, DateTime.Now, source, category, severity));
        return true;
    }

    public bool ClearAlarm(string code)
    {
        var existing = _alarms.FirstOrDefault(alarm => alarm.Code == code && alarm.IsActive);
        if (existing is null)
        {
            return false;
        }

        existing.Clear();
        return true;
    }
}
