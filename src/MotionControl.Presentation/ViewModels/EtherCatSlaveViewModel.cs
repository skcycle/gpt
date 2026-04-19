using MotionControl.Device.Abstractions.Models;

namespace MotionControl.Presentation.ViewModels;

public sealed class EtherCatSlaveViewModel
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
}
