using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using RimWorld.Utility;
using Verse;
using static ACC_ApparelContainerCore.ACC_Utility.ReloadUtils;

namespace ACC_ApparelContainerCore.FloatMenuOptions;

public class FloatMenuOptionProvider_GenericPackForApparel_ReloadInside: FloatMenuOptionProvider_ReloadInsideContainer<Apparel,
    CompProperties_GenericPackForApparel, Comp_GenericPackForApparel>
{
    protected override IEnumerable<IReloadableComp> GetReloadablesUsingAmmoInsideContainer(Pawn pawn, Thing clickedThing)
    {
        if (pawn.apparel == null) yield break;
        var allReloadableComps = GetCompApparelReloadableInGenericPackForApparel(pawn);
        
        foreach (var reloadableComp in allReloadableComps)
        {
            if (reloadableComp != null && clickedThing.def == reloadableComp.AmmoDef)
            {
                yield return reloadableComp;
            }
        }
    }
}