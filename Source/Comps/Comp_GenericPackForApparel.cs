using ACC_ApparelContainerCore.Comps.Props;
using ACC_ApparelContainerCore.DefOfs;
using ACC_ApparelContainerCore.Dialog;
using static ACC_ApparelContainerCore.ACC_Utility.UtilityChecker;
using RimWorld;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.Comps;

public class Comp_GenericPackForApparel : Comp_ThingHolderContainer<Apparel, CompProperties_GenericPackForApparel>
{
    private static bool IsFunctionalUtility(Thing thing)
    {
        if (thing is not Apparel apparel) return false;
        if (IsThingDefHasVerb(thing)) return true;
        if (IsThingHasFunctionalComp(apparel)) return true;
        return false;
    }

    public override bool IsValidTargetToLoad(Thing thingToLoad)
    {
        if (!IsValidTargetToLoadBase(thingToLoad)) return false;
        if (thingToLoad.IsForbidden(Faction.OfPlayer) || thingToLoad.IsForbidden(Wearer)) return false;
        // 不许套娃
        if (thingToLoad == this.parent) return false;
        return IsFunctionalUtility(thingToLoad);
    }


    protected override IEnumerable<Gizmo> GetContainerGizmos()
    {
        yield return new Command_Action
        {
            defaultLabel = "Manage Pack",
            defaultDesc = "loading/unloading pack",
            action = OpenManagementMenu
        };
    }

    /**
     * 在这里代理容器内物品的Gizmo，在代理之前需要调用 SetOwner
     */
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
                SetOwner(twc.holdingOwner, trueTracker);

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

        options.Add(new FloatMenuOption("open menu", OpenPicker));
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


    // 物品选择器
    private void OpenPicker()
    {
        Map currentMap = parent.MapHeld;

        if (currentMap == null || Wearer == null) return;


        var itemsOnMap = currentMap.listerThings.ThingsInGroup(ThingRequestGroup.Apparel)
            .OfType<Apparel>()
            .Where(t => t != null && IsValidTargetToLoad(t));


        var window = new Dialog_ItemSelect<Apparel>(itemsOnMap, ContainedThings, Props.storageCapacity,
            (loadList, unloadList) =>
            {
                // 1. 立即执行卸载 (逻辑上优先)
                foreach (var item in unloadList)
                {
                    TryDrop(item.thing, parent.PositionHeld, currentMap, ThingPlaceMode.Near, item.count, out _);
                }

                // 2. 创建装载的 job
                foreach (var item in loadList)
                {
                    Job loadJob = JobMaker.MakeJob(ACC_JobDefOfs.ACC_Job_PutInGenericPackForApparel, item.thing,
                        parent);
                    loadJob.count = item.count < 1 ? 1 : item.count;

                    Wearer.jobs.jobQueue.EnqueueFirst(loadJob);
                }

                Wearer.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });

        Find.WindowStack.Add(window);
    }
}