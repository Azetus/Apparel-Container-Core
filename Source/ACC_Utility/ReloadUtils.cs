using ACC_ApparelContainerCore.Comps;
using RimWorld;
using RimWorld.Utility;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.ACC_Utility;

public static class ReloadUtils
{
    /**
     * Only for "CompApparelReloadable" in "Comp_GenericPackForApparel"
     */
    public static List<IReloadableComp> GetCompApparelReloadableInGenericPackForApparel(Pawn pawn)
    {
        if (pawn.apparel == null) return new List<IReloadableComp>();

        return pawn.apparel.WornApparel
            .Select(apparel => apparel.TryGetComp<Comp_GenericPackForApparel>())
            .Where(comp => comp != null)
            .SelectMany(container => container.GetDirectlyHeldThings())
            .OfType<ThingWithComps>()
            .Select<ThingWithComps, IReloadableComp>(inner => inner.TryGetComp<CompApparelReloadable>())
            .Where(rel => rel != null)
            // .Where(rel => rel != null && rel.NeedsReload(true))
            .ToList();
    }

    public static void TryGenerateReloadJobs(Pawn pawn, List<IReloadableComp> reloadableCompsList)
    {
        if (reloadableCompsList.NullOrEmpty()) return;

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            return;
        }

        for (var i = 0; i < reloadableCompsList.Count; i++)
        {
            var reloadableComp = reloadableCompsList[i];
            if (reloadableComp == null)
                continue;

            if (pawn.carryTracker.AvailableStackSpace(reloadableComp.AmmoDef) < reloadableComp.MinAmmoNeeded(allowForcedReload: true))
                continue;

            List<Thing> list = ReloadableUtility.FindEnoughAmmo(pawn, pawn.Position, reloadableComp, forceReload: true);
            if (list.NullOrEmpty())
                continue;

            Job reloadJob = JobGiver_Reload.MakeReloadJob(reloadableComp, list);
            if (reloadJob != null)
            {
                reloadJob.playerForced = true;
                if (i == 0)
                    pawn.jobs.TryTakeOrderedJob(reloadJob, JobTag.Misc);
                else
                    pawn.jobs.jobQueue.EnqueueFirst(reloadJob, JobTag.Misc);
            }
        }
    }
}