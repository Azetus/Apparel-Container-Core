using ACC_ApparelContainerCore.Comps;
using RimWorld;
using RimWorld.Utility;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.ACC_Utility;

public static class ReloadUtils
{
    public static IEnumerable<IReloadableComp> GetReloadableCompsInContainer(Pawn pawn)
    {
        if (pawn.apparel == null) yield break;

        // 检测身上穿着的服装
        foreach (Apparel item in pawn.apparel.WornApparel)
        {
            // 检测服装是否作存在 Comp_ThingHolderContainer ，并扫描其内部物品
            var containerComp = item.TryGetComp<Comp_GenericPackForApparel>();
            if (containerComp != null)
            {
                foreach (Thing innerThing in containerComp.GetDirectlyHeldThings())
                {
                    if (innerThing is ThingWithComps innerWithComps)
                    {
                        IReloadableComp? innerRel = innerWithComps.TryGetComp<CompApparelReloadable>();
                        if (innerRel != null && innerRel.NeedsReload(true))
                        {
                            yield return innerRel;
                        }
                    }
                }
            }
        }
    }

    public static void TryGenerateReloadJobs(Pawn pawn, List<IReloadableComp> reloadableCompsList)
    {
        if (reloadableCompsList == null) return;

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