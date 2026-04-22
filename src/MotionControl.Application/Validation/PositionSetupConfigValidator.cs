using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Validation;

public static class PositionSetupConfigValidator
{
    public static void Validate(IReadOnlyCollection<PositionSetupConfigItem> items)
    {
        if (items.Any(item => string.IsNullOrWhiteSpace(item.Name)))
        {
            throw new InvalidOperationException("位置设定名称不能为空");
        }

        var duplicateName = items.GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicateName is not null)
        {
            throw new InvalidOperationException($"位置设定名称重复: {duplicateName.Key}");
        }

        foreach (var item in items)
        {
            if (item.SafeZ < 0)
            {
                throw new InvalidOperationException($"位置设定 {item.Name} SafeZ 不能为负数");
            }

            var configuredAxes = new[] { item.XxAxisNo, item.XAxisNo, item.YAxisNo, item.ZAxisNo, item.UAxisNo, item.VAxisNo, item.WAxisNo }
                .Where(axis => axis >= 0)
                .ToList();

            var duplicateAxis = configuredAxes
                .GroupBy(axis => axis)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateAxis is not null)
            {
                throw new InvalidOperationException($"位置设定 {item.Name} 存在重复轴号: {duplicateAxis.Key}");
            }

            if (configuredAxes.Count == 0)
            {
                throw new InvalidOperationException($"位置设定 {item.Name} 至少需要配置一个轴号");
            }

            if (item.ZAxisNo < 0 && item.SafeZ != 0)
            {
                throw new InvalidOperationException($"位置设定 {item.Name} 未配置 Z 轴时，SafeZ 必须为 0");
            }

            if (item.ZAxisNo >= 0 && item.SafeZ < item.ZPosition)
            {
                throw new InvalidOperationException($"位置设定 {item.Name} 的 SafeZ 不能低于目标 Z");
            }
        }
    }
}
