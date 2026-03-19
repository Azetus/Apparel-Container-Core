using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using RimWorld.Utility;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.JobGivers;

public abstract class JobGiver_ReloadInsideContainer<TThing, TProps, TComp> : ThinkNode_JobGiver
    where TThing : Thing
    where TProps : CompProperties_ThingHolderContainer
    where TComp : Comp_ThingHolderContainer<TThing, TProps>
{
    private const bool ForceReloadWhenLookingForWork = false;

    public override float GetPriority(Pawn pawn)
    {
        return 5.9f;
    }

    public override Job TryGiveJob(Pawn pawn)
    {
        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            return null;
        }
        IReloadableComp? reloadableComp = FindSomeReloadableComponentInContainer(pawn, allowForcedReload: false);
        if (reloadableComp == null)
        {
            return null;
        }
        if (pawn.carryTracker.AvailableStackSpace(reloadableComp.AmmoDef) < reloadableComp.MinAmmoNeeded(allowForcedReload: true))
        {
            return null;
        }
        List<Thing> list = ReloadableUtility.FindEnoughAmmo(pawn, pawn.Position, reloadableComp, forceReload: false);
        if (list.NullOrEmpty())
        {
            return null;
        }

        return JobGiver_Reload.MakeReloadJob(reloadableComp, list);
    }

    public static IReloadableComp? FindSomeReloadableComponentInContainer(Pawn pawn, bool allowForcedReload = false)
    {

        if (pawn.apparel != null)
        {
            foreach (Apparel item in pawn.apparel.WornApparel)
            {
                // 检测服装是否存在 Comp_ThingHolderContainer ，并扫描其内部物品
                var containerComp = item.TryGetComp<TComp>();
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