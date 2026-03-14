using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using RimWorld.Utility;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.FloatMenuOptions;

// 处理重装填容器内部物品
public abstract class FloatMenuOptionProvider_ReloadInsideContainer<TThing, TProps, TComp> : FloatMenuOptionProvider
    where TThing : Thing
    where TProps : CompProperties_ThingHolderContainer
    where TComp : Comp_ThingHolderContainer<TThing, TProps>
{
    public override bool Drafted => true;

    public override bool Undrafted => true;

    public override bool Multiselect => false;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        foreach (IReloadableComp reloadable in GetReloadablesUsingAmmoInsideContainer(context.FirstSelectedPawn, clickedThing))
        {
            if (reloadable is ThingComp thingComp)
            {
                string text = "Reload".Translate(thingComp.parent.Named("GEAR"), NamedArgumentUtility.Named(reloadable.AmmoDef, "AMMO")) + " (" +
                              reloadable.LabelRemaining + ")";
                // 这部分基本上完全沿用了原版的实现
                if (!context.FirstSelectedPawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    yield return new FloatMenuOption(text + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                    continue;
                }

                if (!reloadable.NeedsReload(allowForceReload: true))
                {
                    yield return new FloatMenuOption(text + ": " + "ReloadFull".Translate(), null);
                    continue;
                }

                List<Thing> chosenAmmo;

                if ((chosenAmmo = ReloadableUtility.FindEnoughAmmo(context.FirstSelectedPawn, clickedThing.Position, reloadable,
                        forceReload: true)) == null)
                {
                    yield return new FloatMenuOption(text + ": " + "ReloadNotEnough".Translate(), null);
                    continue;
                }

                if (context.FirstSelectedPawn.carryTracker.AvailableStackSpace(reloadable.AmmoDef) <
                    reloadable.MinAmmoNeeded(allowForcedReload: true))
                {
                    yield return new FloatMenuOption(
                        text + ": " + "ReloadCannotCarryEnough".Translate(NamedArgumentUtility.Named(reloadable.AmmoDef, "AMMO")), null);
                    continue;
                }

                yield return FloatMenuUtility.DecoratePrioritizedTask(
                    new FloatMenuOption(text,
                        () =>
                        {
                            context.FirstSelectedPawn.jobs.TryTakeOrderedJob(JobGiver_Reload.MakeReloadJob(reloadable, chosenAmmo), JobTag.Misc);
                        },
                        priority: MenuOptionPriority.RescueOrCapture
                    ),
                    context.FirstSelectedPawn, clickedThing);
            }
        }
    }

    protected virtual IEnumerable<IReloadableComp> GetReloadablesUsingAmmoInsideContainer(Pawn pawn, Thing clickedThing)
    {
        if (pawn.apparel == null) yield break;

        // 检测身上穿着的服装
        foreach (Apparel item in pawn.apparel.WornApparel)
        {
            // 检测服装是否作存在 Comp_ThingHolderContainer ，并扫描其内部物品
            var containerComp = item.TryGetComp<TComp>();
            if (containerComp != null)
            {
                foreach (Thing innerThing in containerComp.GetDirectlyHeldThings())
                {
                    if (innerThing is ThingWithComps innerWithComps)
                    {
                        IReloadableComp? innerRel = innerWithComps.TryGetComp<CompApparelReloadable>();
                        if (innerRel != null && clickedThing.def == innerRel.AmmoDef)
                        {
                            yield return innerRel;
                        }
                    }
                }
            }
        }
    }
}