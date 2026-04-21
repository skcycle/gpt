using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Validation;

public static class CylinderConfigValidator
{
    public static void Validate(IReadOnlyCollection<CylinderConfigItem> items)
    {
        if (items.Any(item => string.IsNullOrWhiteSpace(item.Name)))
        {
            throw new InvalidOperationException("Cylinder 名称不能为空");
        }

        if (items.Any(item => item.ExtendSensorInputAddress < 0 || item.RetractSensorInputAddress < 0 || item.ExtendOutputAddress < 0 || item.RetractOutputAddress < 0))
        {
            throw new InvalidOperationException("Cylinder 地址不能小于 0");
        }

        var duplicateName = items.GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicateName is not null)
        {
            throw new InvalidOperationException($"Cylinder 名称重复: {duplicateName.Key}");
        }
    }
}
