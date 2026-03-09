using System.Reflection;
using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.ACC_Utility;

[StaticConstructorOnStartup]
public static class UtilityChecker
{
    private static readonly Dictionary<Type, bool> _cache = new Dictionary<Type, bool>();

    // 检查 ThingComp 类型是否具有潜在的功能性（重写了 Gizmo 方法）
    public static bool IsCompPotentiallyFunctional(Type type)
    {
        // var method = type.GetMethod(
        //     "CompGetWornGizmosExtra",
        //     BindingFlags.Instance | BindingFlags.Public
        // );
        // bool isFunctional = method != null && method.GetBaseDefinition().DeclaringType != method.DeclaringType;

        if (_cache.TryGetValue(type, out bool result)) return result;
        // 如果重写了"CompGetWornGizmosExtra"就视为功能性组件
        var methodWorn = type.GetMethod(
            nameof(ThingComp.CompGetWornGizmosExtra),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        bool isFunctional = methodWorn != null && methodWorn.DeclaringType != typeof(ThingComp);
        return _cache[type] = isFunctional;
    }


    public static bool IsFunctionalThing(Thing thing)
    {
        if (thing is not ThingWithComps thingWithComps) return false;
        return thingWithComps.AllComps.Any(comp => IsCompPotentiallyFunctional(comp.GetType()));
    }

    // 判断下CompUsable和CompRechargeable基本上就能满足要求了
    public static bool IsApparelHasCompUsableOrRechargeable(ThingWithComps twc)
    {
        return twc.HasComp<CompRechargeable>() || twc.HasComp<CompUsable>();
    }
}