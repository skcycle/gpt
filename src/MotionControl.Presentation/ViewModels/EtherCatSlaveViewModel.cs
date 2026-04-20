using MotionControl.Device.Abstractions.Models;

namespace MotionControl.Presentation.ViewModels;

public sealed class EtherCatSlaveViewModel : IEquatable<EtherCatSlaveViewModel>
{
    public EtherCatSlaveViewModel(EtherCatSlaveStatus slaveStatus)
    {
        SlaveNo = slaveStatus.SlaveNo;
        Name = slaveStatus.Name;
        State = slaveStatus.State;
        ModuleType = slaveStatus.ModuleType;
        FaultText = slaveStatus.FaultText;
        IsOnline = slaveStatus.IsOnline;
        HasAlarm = slaveStatus.HasAlarm;
    }

    public int SlaveNo { get; }
    public string Name { get; }
    public string State { get; }
    public string ModuleType { get; }
    public string FaultText { get; }
    public bool IsOnline { get; }
    public bool HasAlarm { get; }

    public bool Equals(EtherCatSlaveViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return SlaveNo == other.SlaveNo
            && Name == other.Name
            && State == other.State
            && ModuleType == other.ModuleType
            && FaultText == other.FaultText
            && IsOnline == other.IsOnline
            && HasAlarm == other.HasAlarm;
    }

    public override bool Equals(object? obj) => Equals(obj as EtherCatSlaveViewModel);

    public override int GetHashCode()
    {
        return HashCode.Combine(SlaveNo, Name, State, ModuleType, FaultText, IsOnline, HasAlarm);
    }
}
