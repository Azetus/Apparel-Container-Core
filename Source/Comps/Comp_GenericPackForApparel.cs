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
        options.Add(new FloatMenuOption("loading item from Map...", () =>
        {
            Find.Targeter.BeginTargeting(GetTargetingParameters(), target =>
            {
                if (target.HasThing && Wearer != null)
                {
                    // 注意：JobDef 需确保能处理这个 parent (ThingWithComps)
                    Job job = JobMaker.MakeJob(ACC_JobDefOfs.ACC_Job_PutInGenericPackForApparel, target.Thing, parent);
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
                // 基础类别过滤：必须是 Item 类别
                if (thing.def.category != ThingCategory.Item) return false;
                // 状态检查：必须在地图上，且未被销毁
                if (!thing.Spawned || thing.Destroyed) return false;
                // 权限检查：是否被禁用了，或者正在被其他人交互
                if (thing.IsForbidden(Wearer) || thing.IsBurning()) return false;

                // 因为在调用comp时设置了Owner为Pawn_ApparelTracker，只允许接受Apparel
                if (thing is not Apparel) return false;

                // TODO (预留) 白名单/黑名单逻辑
                // 应该改成白名单机制，

                // 不许套娃
                if (thing == this.parent) return false;
                if (thing is IThingHolder) return false;

                return true;
            }
        };
    }
}