using System.Reflection;
using ACC_ApparelContainerCore.Settings;
using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.ACC_Utility;

public static class UtilityChecker
{
    #region ThingDef Check

    public static bool IsThingDefBeltLayer(ThingDef def)
    {
        if (def?.apparel == null) return false;
        var layers = def.apparel.layers;
        if (layers.NullOrEmpty()) return false;
        return layers.Count == 1 && layers.Contains(ApparelLayerDefOf.Belt);
    }

    public static bool IsThingDefHasAbility(ThingDef def)
    {
        return !def?.apparel?.abilities.NullOrEmpty() ?? false;
    }

    public static bool IsThingDefHasVerb(ThingDef def)
    {
        return !def?.Verbs?.NullOrEmpty() ?? false;
    }

    public static bool IsThingDefHasFunctionalCompProperties(ThingDef def)
    {
        return def?.comps?.Any(c =>
            c is CompProperties_Usable or CompProperties_ApparelReloadable or CompProperties_Rechargeable) ?? false;
    }

    #endregion

    #region Thing Check

    public static bool IsThingBeltLayer(Thing thing)
    {
        return IsThingDefBeltLayer(thing.def);
    }

    // 判断下是不是有Ability的Apparel
    public static bool IsApparelHasAbility(Thing thing)
    {
        if (thing is not Apparel apparel) return false;
        return !apparel.AllAbilitiesForReading.EnumerableNullOrEmpty();
    }

    public static bool IsThingHasVerb(Thing thing)
    {
        ThingDef def = thing.def;
        return !def.Verbs.NullOrEmpty();
    }

    // 判断下CompUsable和CompRechargeable基本上就能满足要求了
    public static bool IsThingHasFunctionalComp(Thing thing)
    {
        if (thing is not ThingWithComps twc) return false;
        return twc.HasComp<CompUsable>() || twc.HasComp<CompApparelReloadable>() || twc.HasComp<CompRechargeable>();
    }

    #endregion

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

        bool thingCheck = IsThingBeltLayer(ThingOfT) &&
                          (IsThingHasVerb(ThingOfT) || IsThingHasFunctionalComp(ThingOfT) || IsApparelHasAbility(ThingOfT));

        return thingCheck || SettingUtils.IsInWhitelist(thing.def);
    }

    public static bool IsDefFunctionalUtility(ThingDef def)
    {
        return IsThingDefHasAbility(def) || IsThingDefHasVerb(def) || IsThingDefHasFunctionalCompProperties(def);
    }
}