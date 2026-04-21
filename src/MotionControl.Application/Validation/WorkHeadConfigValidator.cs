using MotionControl.Infrastructure.Configuration;

namespace MotionControl.Application.Validation;

public static class WorkHeadConfigValidator
{
    public static void Validate(IReadOnlyCollection<WorkHeadConfigItem> items)
    {
        if (items.Any(item => string.IsNullOrWhiteSpace(item.Name)))
        {
            throw new InvalidOperationException("WorkHead 名称不能为空");
        }

        if (items.Any(item => item.VacuumOutputAddress < -1 || item.BlowOutputAddress < -1 || item.VacuumInputAddress < -1))
        {
            throw new InvalidOperationException("WorkHead DI/DO 地址不能小于 -1");
        }

        var duplicateName = items.GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicateName is not null)
        {
            throw new InvalidOperationException($"WorkHead 名称重复: {duplicateName.Key}");
        }

        foreach (var item in items)
        {
            if (item.VacuumOutputAddress >= 0 && item.VacuumOutputAddress == item.BlowOutputAddress)
            {
                throw new InvalidOperationException($"WorkHead {item.Name} 的 Vacuum DO 和 Blow DO 不能相同");
            }

            if (item.VacuumInputAddress >= 0 && (item.VacuumInputAddress == item.VacuumOutputAddress || item.VacuumInputAddress == item.BlowOutputAddress))
            {
                throw new InvalidOperationException($"WorkHead {item.Name} 的 Vacuum DI 不能和 DO 重复");
            }
        }

        ValidateUnique(items.Select(i => (i.Name, i.VacuumOutputAddress, Label: "Vacuum DO")));
        ValidateUnique(items.Select(i => (i.Name, i.BlowOutputAddress, Label: "Blow DO")));
        ValidateUnique(items.Select(i => (i.Name, i.VacuumInputAddress, Label: "Vacuum DI")));
    }

    private static void ValidateUnique(IEnumerable<(string Name, int Address, string Label)> refs)
    {
        var duplicate = refs.Where(x => x.Address >= 0).GroupBy(x => x.Address).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            var details = string.Join(", ", duplicate.Select(x => $"{x.Name}:{x.Label}"));
            throw new InvalidOperationException($"WorkHead IO 地址重复: {duplicate.Key}，涉及 {details}");
        }
    }
}
