namespace MotionControl.Domain.Entities;

public sealed class IoPoint
{
    public IoPoint(string name, int address, bool isOutput)
    {
        Name = name;
        Address = address;
        IsOutput = isOutput;
    }

    public string Name { get; }
    public int Address { get; }
    public bool IsOutput { get; }
    public bool Value { get; private set; }

    public void Update(bool value) => Value = value;
}
