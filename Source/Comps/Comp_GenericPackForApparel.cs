using ACC_ApparelContainerCore.Comps.Props;
using ACC_ApparelContainerCore.DefOfs;
using RimWorld;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.Comps;

public class Comp_GenericPackForApparel : Comp_ThingHolderContainer<Apparel, CompProperties_GenericPackForApparel>
{
    protected override IEnumerable<Gizmo> GetContainerGizmos()
    {
        yield return new Command_Action
        {
            defaultLabel = "Manage Pack",
            defaultDesc = "loading/unloading pack",
            action = OpenManagementMenu
        };
    }

    protected override IEnumerable<Gizmo> GetExtraGizmosInContainer()
    {
        // 转发子物品的 Gizmo
        if (GetDirectlyHeldThings() == null || Wearer == null) yield break;
        Pawn_ApparelTracker trueTracker = Wearer.apparel;
        int itemCounter = 1;
        foreach (Apparel subItem in InnerContainer)
        {
            if (subItem is ThingWithComps twc)
            {
                // 重定向 Owner
                SetOwner(subItem.holdingOwner, trueTracker);

                int currentIndex = itemCounter++;

                foreach (ThingComp comp in twc.AllComps)
                {
                    foreach (Gizmo gizmo in comp.CompGetWornGizmosExtra())
                    {
                        if (ShouldShowGizmo(gizmo))
                            yield return ProcessProxyGizmo(gizmo, twc, currentIndex);
                    }
                }
            }
        }
    }

    private void OpenManagementMenu()
    {
        List<FloatMenuOption> options = new List<FloatMenuOption>();

        // 装载逻辑
        if (CanAcceptMore)
            options.Add(new FloatMenuOption("loading item from Map...", () =>
            {
                Find.Targeter.BeginTargeting(GetTargetingParameters(), target =>
                {
                    if (target.HasThing && Wearer != null)
                    {
                        // 注意：JobDef 需确保能处理这个 parent (ThingWithComps)
                        Job job = JobMaker.MakeJob(ACC_JobDefOfs.ACC_Job_PutInGenericPackForApparel, target.Thing,
                            parent);
                        job.count = 1;
                        Wearer.jobs.TryTakeOrderedJob(job);
                    }
                });
            }));

        // 卸载逻辑
        options.Add(new FloatMenuOption("unloading item...", () =>
        {
            List<FloatMenuOption> unloadOptions = InnerContainer.Select(t => new FloatMenuOption(t.LabelCap, () =>
                {
                    if (TryDrop(t, parent.PositionHeld, parent.MapHeld, ThingPlaceMode.Near, out _))
                    {
                        Messages.Message($"dropping {t.LabelCap}", MessageTypeDefOf.NeutralEvent);
                    }
                }))
                .ToList();

            if (unloadOptions.Count == 0) unloadOptions.Add(new FloatMenuOption("pack is empty", null));

            Find.WindowStack.Add(new FloatMenu(unloadOptions));
        }));

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private TargetingParameters GetTargetingParameters()
    {
        return new TargetingParameters
        {
            canTargetItems = true,
            canTargetPawns = false,
            canTargetBuildings = false,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = (TargetInfo t) =>
            {
                if (!t.HasThing) return false;
                var thing = t.Thing;
                return IsValidTargetToLoad(thing);
            }
        };
    }
}