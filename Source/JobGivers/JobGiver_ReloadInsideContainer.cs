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

    protected abstract IReloadableComp? FindSomeReloadableComponentInContainer(Pawn pawn, bool allowForcedReload = false);
}