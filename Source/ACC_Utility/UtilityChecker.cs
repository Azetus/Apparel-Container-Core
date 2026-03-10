using System.Reflection;
using ACC_ApparelContainerCore.Settings;
using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.ACC_Utility;

public static class UtilityChecker
{
    // 判断下CompUsable和CompRechargeable基本上就能满足要求了
    public static bool IsThingHasFunctionalComp(Thing thing)
    {
        if (thing is not ThingWithComps twc) return false;
        return twc.HasComp<CompUsable>() || twc.HasComp<CompApparelReloadable>() || twc.HasComp<CompRechargeable>();
    }

    public static bool IsThingDefHasVerb(Thing thing)
    {
        ThingDef def = thing.def;
        return !def.Verbs.NullOrEmpty();
    }

    public static bool IsCompPotentiallyFunctional(Type type)
    {
        // 如果重写了"CompGetWornGizmosExtra"就视为功能性组件
        var methodWorn = type.GetMethod(
            nameof(ThingComp.CompGetWornGizmosExtra),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        return methodWorn != null && methodWorn.DeclaringType != typeof(ThingComp);
    }

    public static bool IsFunctionalUtility<T>(Thing thing) where T : Thing
    {
        if (thing is not T ThingOfT) return false;

        if (SettingUtils.IsInBlacklist(thing.def)) return false;

        if (SettingUtils.IsUsingStrictWhitelistMode)
            return SettingUtils.IsInWhitelist(thing.def);

        return IsThingDefHasVerb(thing) || IsThingHasFunctionalComp(ThingOfT) || SettingUtils.IsInWhitelist(thing.def);
    }
}