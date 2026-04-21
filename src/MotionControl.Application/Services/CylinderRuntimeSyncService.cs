using System.Linq;
using MotionControl.Application.Interfaces;
using MotionControl.Domain.Entities;
using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Services;

public sealed class CylinderRuntimeSyncService(Machine machine) : ICylinderRuntimeSyncService
{
    public Task ApplyAsync(CylinderConfigItem cylinder, CancellationToken cancellationToken = default)
    {
        var existing = machine.Cylinders.FirstOrDefault(item => string.Equals(item.Name, cylinder.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            machine.AddCylinder(new Cylinder(cylinder.Name, cylinder.ExtendSensorInputAddress, cylinder.RetractSensorInputAddress, cylinder.ExtendOutputAddress, cylinder.RetractOutputAddress, cylinder.Description, cylinder.ActionTimeoutMs));
            return Task.CompletedTask;
        }

        existing.UpdateMetadata(cylinder.Name, cylinder.ExtendSensorInputAddress, cylinder.RetractSensorInputAddress, cylinder.ExtendOutputAddress, cylinder.RetractOutputAddress, cylinder.Description, cylinder.ActionTimeoutMs);
        return Task.CompletedTask;
    }

    public Task ReloadAsync(IEnumerable<CylinderConfigItem> cylinders, CancellationToken cancellationToken = default)
    {
        foreach (var item in machine.Cylinders.ToList())
        {
            machine.RemoveCylinder(item.Name);
        }

        foreach (var item in cylinders)
        {
            machine.AddCylinder(new Cylinder(item.Name, item.ExtendSensorInputAddress, item.RetractSensorInputAddress, item.ExtendOutputAddress, item.RetractOutputAddress, item.Description, item.ActionTimeoutMs));
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        machine.RemoveCylinder(name);
        return Task.CompletedTask;
    }
}
