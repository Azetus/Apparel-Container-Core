using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.ACC_Utility;

public static class UtilityChecker
{
    // 判断下CompUsable和CompRechargeable基本上就能满足要求了
    public static bool IsApparelHasCompUsableOrRechargeable(ThingWithComps twc)
    {
        return twc.HasComp<CompRechargeable>() || twc.HasComp<CompUsable>();
    }

    public static bool IsThingDefHasVerb(Thing thing)
    {
        ThingDef def = thing.def;
        return !def.Verbs.NullOrEmpty();
    }
}