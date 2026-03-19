using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using RimWorld.Utility;
using Verse;

namespace ACC_ApparelContainerCore.JobGivers;

public class JobGiver_GenericPackForApparel_ReloadInside : JobGiver_ReloadInsideContainer<Apparel,
    CompProperties_GenericPackForApparel, Comp_GenericPackForApparel>
{
    protected override IReloadableComp? FindSomeReloadableComponentInContainer(Pawn pawn, bool allowForcedReload = false)
    {

        if (pawn.apparel != null)
        {
            foreach (Apparel item in pawn.apparel.WornApparel)
            {
                // 检测服装是否存在 Comp_ThingHolderContainer ，并扫描其内部物品
                var containerComp = item.TryGetComp<Comp_GenericPackForApparel>();
                if (containerComp != null)
                {
                    foreach (Thing innerThing in containerComp.GetDirectlyHeldThings())
                    {
                        if (innerThing is ThingWithComps innerWithComps)
                        {
                            IReloadableComp? compApparelReloadable = innerWithComps.TryGetComp<CompApparelReloadable>();
                            if (compApparelReloadable != null && compApparelReloadable.NeedsReload(allowForcedReload))
                            {
                                return compApparelReloadable;
                            }
                        }
                    }
                }
            }
        }
        return null;
    }
}