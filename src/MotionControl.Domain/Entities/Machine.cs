using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Entities;

public sealed class Machine
{
    private readonly List<Axis> _axes;
    private readonly List<IoPoint> _ioPoints;
    private readonly List<Cylinder> _cylinders;
    private readonly List<WorkHead> _workHeads;
    private readonly List<Alarm> _alarms;

    public Machine(
        IReadOnlyCollection<Axis> axes,
        IReadOnlyCollection<AxisGroup> groups,
        IReadOnlyCollection<IoPoint>? ioPoints = null,
        IReadOnlyCollection<Cylinder>? cylinders = null,
        IReadOnlyCollection<WorkHead>? workHeads = null,
        IReadOnlyCollection<Alarm>? alarms = null)
    {
        _axes = axes.ToList();
        Groups = groups;
        _ioPoints = ioPoints?.ToList() ?? new List<IoPoint>();
        _cylinders = cylinders?.ToList() ?? new List<Cylinder>();
        _workHeads = workHeads?.ToList() ?? new List<WorkHead>();
        _alarms = alarms?.ToList() ?? new List<Alarm>();
    }

    public IReadOnlyCollection<Axis> Axes => _axes;
    public IReadOnlyCollection<AxisGroup> Groups { get; }
    public IReadOnlyCollection<IoPoint> IoPoints => _ioPoints;
    public IReadOnlyCollection<Cylinder> Cylinders => _cylinders;
    public IReadOnlyCollection<WorkHead> WorkHeads => _workHeads;
    public IReadOnlyCollection<Alarm> Alarms => _alarms;
    public SystemState CurrentState { get; private set; } = SystemState.Initializing;
    public bool IsConnected { get; private set; }

    public void SetSystemState(SystemState state) => CurrentState = state;

    public void SetConnected(bool connected) => IsConnected = connected;

    public void AddAxis(Axis axis)
    {
        if (_axes.Any(existing => existing.Id.Value == axis.Id.Value))
        {
            return;
        }

        _axes.Add(axis);
    }

    public bool RemoveAxis(int axisNo)
    {
        var axis = _axes.FirstOrDefault(item => item.Id.Value == axisNo);
        if (axis is null)
        {
            return false;
        }

        _axes.Remove(axis);
        return true;
    }

    public void AddIoPoint(IoPoint ioPoint)
    {
        if (_ioPoints.Any(existing => existing.IsOutput == ioPoint.IsOutput && existing.Address == ioPoint.Address))
        {
            return;
        }

        _ioPoints.Add(ioPoint);
    }

    public bool RemoveIoPoint(bool isOutput, int address)
    {
        var ioPoint = _ioPoints.FirstOrDefault(item => item.IsOutput == isOutput && item.Address == address);
        if (ioPoint is null)
        {
            return false;
        }

        _ioPoints.Remove(ioPoint);
        return true;
    }

    public void AddCylinder(Cylinder cylinder)
    {
        if (_cylinders.Any(existing => string.Equals(existing.Name, cylinder.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _cylinders.Add(cylinder);
    }

    public bool RemoveCylinder(string name)
    {
        var cylinder = _cylinders.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (cylinder is null)
        {
            return false;
        }

        _cylinders.Remove(cylinder);
        return true;
    }

    public void AddWorkHead(WorkHead workHead)
    {
        if (_workHeads.Any(existing => string.Equals(existing.Name, workHead.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _workHeads.Add(workHead);
    }

    public bool RemoveWorkHead(string name)
    {
        var workHead = _workHeads.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (workHead is null)
        {
            return false;
        }

        _workHeads.Remove(workHead);
        return true;
    }

    public bool UpsertAlarm(string code, string message, string source = "System", string category = "General", string severity = "Error")
    {
        var now = DateTime.Now;
        var existing = _alarms.FirstOrDefault(alarm => alarm.Code == code && alarm.IsActive);
        if (existing is not null)
        {
            existing.Update(message, now, source, category, severity);
            return false;
        }

        _alarms.RemoveAll(alarm => alarm.Code == code && !alarm.IsActive);
        _alarms.Add(new Alarm(code, message, now, source, category, severity));
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
