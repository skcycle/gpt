using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Entities;

public sealed class Cylinder
{
    public Cylinder(string name, int extendSensorInputAddress, int retractSensorInputAddress, int extendOutputAddress, int retractOutputAddress, string description = "")
    {
        Name = name;
        ExtendSensorInputAddress = extendSensorInputAddress;
        RetractSensorInputAddress = retractSensorInputAddress;
        ExtendOutputAddress = extendOutputAddress;
        RetractOutputAddress = retractOutputAddress;
        Description = description;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public int ExtendSensorInputAddress { get; private set; }
    public int RetractSensorInputAddress { get; private set; }
    public int ExtendOutputAddress { get; private set; }
    public int RetractOutputAddress { get; private set; }
    public CylinderState State { get; private set; } = CylinderState.Unknown;

    public void UpdateMetadata(string name, int extendSensorInputAddress, int retractSensorInputAddress, int extendOutputAddress, int retractOutputAddress, string description)
    {
        Name = name;
        ExtendSensorInputAddress = extendSensorInputAddress;
        RetractSensorInputAddress = retractSensorInputAddress;
        ExtendOutputAddress = extendOutputAddress;
        RetractOutputAddress = retractOutputAddress;
        Description = description;
    }

    public void UpdateState(bool extendSensorOn, bool retractSensorOn, bool extendOutputOn, bool retractOutputOn)
    {
        if (extendSensorOn && retractSensorOn)
        {
            State = CylinderState.Conflict;
            return;
        }

        if (extendSensorOn)
        {
            State = CylinderState.Extended;
            return;
        }

        if (retractSensorOn)
        {
            State = CylinderState.Retracted;
            return;
        }

        if (extendOutputOn && !retractOutputOn)
        {
            State = CylinderState.Extending;
            return;
        }

        if (retractOutputOn && !extendOutputOn)
        {
            State = CylinderState.Retracting;
            return;
        }

        State = CylinderState.Unknown;
    }
}
