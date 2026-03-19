using ACC_ApparelContainerCore.Commands;
using ACC_ApparelContainerCore.Comps.Props;
using ACC_ApparelContainerCore.Dialog;
using RimWorld;
using UnityEngine;
using Verse;
using static ACC_ApparelContainerCore.ACC_Utility.UtilityChecker;
using static ACC_ApparelContainerCore.ACC_Utility.ReloadUtils;

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

    /**
     * 重写 CreateManagementGizmo 添加右键重装填配件
     */
    protected override Gizmo CreateManagementGizmo()
    {
        Texture iconTex = Widgets.GetIconFor(
            parent,
            new Vector2(75f, 75f),
            parent.def.defaultPlacingRot,
            stackOfOne: true,
            out _,
            out _,
            out _,
            out Color iconColor,
            out _
        );
        return new Command_ExtraFloatMenu
        {
            defaultLabel = parent.def.label,
            defaultDesc = "ACC_ManagePackGizmo_defaultDesc".Translate(),
            icon = iconTex,
            defaultIconColor = iconColor,
            groupable = false,
            action = () =>
            {
                if (parent.MapHeld == null) return;
                var window = new Dialog_ContainerManagement<Apparel, CompProperties_GenericPackForApparel>(this);
                Find.WindowStack.Add(window);
            },
            floatMenuOptions = () =>
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                if (Wearer is Pawn pawn)
                {
                    var allReloadableCompsEnumerable = GetReloadableCompsInContainer(pawn);
                    var allReloadableComps = allReloadableCompsEnumerable.ToList();
                    
                    if (allReloadableComps.Any())
                    {
                        list.Add(new FloatMenuOption("补充所有消耗品", () => { TryGenerateReloadJobs(pawn, allReloadableComps); }));
                    }
                }
                return list;
            }
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
                            yield return ProcessProxyGizmo(gizmo, currentIndex);
                    }
                }
            }

            // 处理技能，比如主脑节点
            int ablCounter = 1;
            foreach (Ability abl in subItem.AllAbilitiesForReading ?? Enumerable.Empty<Ability>())
            {
                abl.pawn = Wearer;
                abl.verb.caster = Wearer;
                int curAblIndex = ablCounter++;
                foreach (Gizmo gizmo in abl.GetGizmos() ?? Enumerable.Empty<Gizmo>())
                {
                    yield return ProcessProxyGizmo(gizmo, curAblIndex);
                }
            }
        }
    }

    // 处理 ability tick
    private bool _AbilitiesCachedDirty = true;

    private List<Ability> cachedInnerAbilities = new List<Ability>();

    public override void Notify_InnerContainerContentsChanged()
    {
        base.Notify_InnerContainerContentsChanged();
        _AbilitiesCachedDirty = true;
    }

    public List<Ability> AllInnerAbilities
    {
        get
        {
            if (_AbilitiesCachedDirty)
            {
                cachedInnerAbilities.Clear();
                if (InnerContainer != null)
                {
                    for (int i = 0; i < InnerContainer.Count; i++)
                    {
                        var subAbilities = InnerContainer[i].AllAbilitiesForReading;
                        var abilitiesList = subAbilities?.ToList() ?? new List<Ability>();
                        if (abilitiesList.Count > 0)
                            cachedInnerAbilities.AddRange(abilitiesList);
                    }
                }

                _AbilitiesCachedDirty = false;
            }

            return cachedInnerAbilities;
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        // Pawn? wearer = Wearer;
        // if (wearer == null) return;
        var abilities = AllInnerAbilities;
        for (int i = 0; i < abilities.Count; i++)
        {
            Ability abl = abilities[i];
            // abl.pawn = wearer;
            // abl.verb.caster = wearer;
            abl.AbilityTick();
        }
    }
}