using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using RimWorld.Utility;
using Verse;
using static ACC_ApparelContainerCore.ACC_Utility.ReloadUtils;

namespace ACC_ApparelContainerCore.JobGivers;

public class JobGiver_GenericPackForApparel_ReloadInside : JobGiver_ReloadInsideContainer<Apparel,
    CompProperties_GenericPackForApparel, Comp_GenericPackForApparel>
{
    protected override IReloadableComp? FindSomeReloadableComponentInContainer(Pawn pawn, bool allowForcedReload = false)
    {

        if (pawn.apparel != null)
        {
            
            var allReloadableComps = GetCompApparelReloadableInGenericPackForApparel(pawn);
            foreach (var reloadableComp in allReloadableComps)
            {
                if (reloadableComp != null && reloadableComp.NeedsReload(allowForcedReload))
                {
                    return reloadableComp;
                }
            }
        }
        return null;
    }
}