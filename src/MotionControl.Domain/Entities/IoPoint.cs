namespace MotionControl.Domain.Entities;

public sealed class IoPoint
{
    public IoPoint(string name, int address, bool isOutput, string description = "")
    {
        Name = name;
        Address = address;
        IsOutput = isOutput;
        Description = description;
    }

    public string Name { get; private set; }
    public int Address { get; private set; }
    public bool IsOutput { get; }
    public string Description { get; private set; }
    public bool Value { get; private set; }

    public void Update(bool value) => Value = value;
    public void UpdateMetadata(string name, int address, string description)
    {
        Name = name;
        Address = address;
        Description = description;
    }
}
