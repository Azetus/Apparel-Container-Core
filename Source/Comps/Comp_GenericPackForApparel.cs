using ACC_ApparelContainerCore.Comps.Props;
using ACC_ApparelContainerCore.Dialog;
using RimWorld;
using Verse;
using static ACC_ApparelContainerCore.ACC_Utility.UtilityChecker;

namespace ACC_ApparelContainerCore.Comps;

public class Comp_GenericPackForApparel : Comp_ThingHolderContainer<Apparel, CompProperties_GenericPackForApparel>
{
    public override bool IsTargetInteractable(Thing thingToInteract)
    {
        if (thingToInteract.IsForbidden(Faction.OfPlayer) || thingToInteract.IsForbidden(Wearer)) return false;
        return IsValidTargetToLoad(thingToInteract);
    }

    public override bool IsValidTargetToLoad(Thing thingToLoad)
    {
        if (!IsValidTargetToLoadBase(thingToLoad)) return false;
        // 不许套娃
        if (thingToLoad == this.parent) return false;
        return IsFunctionalUtility<Apparel>(thingToLoad);
    }


    protected override IEnumerable<Gizmo> GetContainerGizmos()
    {
        yield return new Command_Action
        {
            defaultLabel = parent.def.label,
            defaultDesc = "ACC_ManagePackGizmo_defaultDesc".Translate(),
            icon = parent.def.uiIcon,
            action = OpenPicker
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


    // 物品选择器
    private void OpenPicker()
    {
        Map currentMap = parent.MapHeld;

        if (currentMap == null || Wearer == null) return;

        var window = new Dialog_ContainerManagement<Apparel, CompProperties_GenericPackForApparel>(this);

        Find.WindowStack.Add(window);
    }
}