using ACC_ApparelContainerCore.Commands;
using ACC_ApparelContainerCore.Comps.Props;
using ACC_ApparelContainerCore.ACC_Utility;
using ACC_ApparelContainerCore.Dialog;
using RimWorld;
using RimWorld.Utility;
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
                if (Wearer is Pawn pawn && this.ContainedThings != null)
                {
                    // 仅装填当前容器内的物品
                    var allReloadableCompsEnumerable = this.ContainedThings
                        .Where(apl => apl != null)
                        .Select<ThingWithComps, IReloadableComp>(inner => inner.TryGetComp<CompApparelReloadable>())
                        .Where(rel => rel != null)
                        .ToList();
                    var allReloadableComps = allReloadableCompsEnumerable.Where(comp => comp.NeedsReload(true)).ToList();
                    
                    if (allReloadableComps.Any())
                    {
                        list.Add(new FloatMenuOption("ACC_ReloadAll_label".Translate(), () => { TryGenerateReloadJobs(pawn, allReloadableComps); }));
                    }
                }
                return list;
            }
        };
    }


    /**
     * 只转发 Gizmo，使用者绑定由容器生命周期维护。
     */
    protected override IEnumerable<Gizmo> GetExtraGizmosInContainer()
    {
        // 转发子物品的 Gizmo
        if (GetDirectlyHeldThings() == null || Wearer == null) yield break;
        int itemCounter = 1;
        foreach (Apparel subItem in InnerContainer)
        {
            if (subItem is ThingWithComps twc)
            {
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
    private readonly HashSet<Apparel> boundItems = new HashSet<Apparel>();
    private Pawn? boundWearer;
    private bool proxyContextDirty = true;
    private bool tickingAbilities;

    public override void Notify_InnerContainerContentsChanged()
    {
        base.Notify_InnerContainerContentsChanged();
        _AbilitiesCachedDirty = true;
        proxyContextDirty = true;
        if (Scribe.mode == LoadSaveMode.Inactive)
            RefreshProxyContext();
    }

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        proxyContextDirty = true;
        RefreshProxyContext();
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        proxyContextDirty = true;
        RefreshProxyContext();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            _AbilitiesCachedDirty = true;
            proxyContextDirty = true;
        }
    }

    private void RefreshProxyContext()
    {
        // Do not clear the pawn of an ability while its own CompTick is still executing.
        if (tickingAbilities || GetDirectlyHeldThings() == null) return;
        Pawn? wearer = parent.Destroyed ? null : Wearer;
        if (!proxyContextDirty && wearer == boundWearer) return;

        SetOwner(GetDirectlyHeldThings(), (IThingHolder?)wearer?.apparel ?? this);
        foreach (Apparel previous in boundItems.ToArray())
        {
            if (!previous.Destroyed && ContainedThings.Contains(previous)) continue;
            // A consumed item may still be executing its verb, and the native UI may
            // still hold its command. Leave that final cast context intact until GC.
            if (!previous.Destroyed)
                BindItem(previous, null);
            boundItems.Remove(previous);
        }

        foreach (Apparel item in InnerContainer)
        {
            if (item.Destroyed) continue;
            BindItem(item, wearer);
            boundItems.Add(item);
        }

        boundWearer = wearer;
        proxyContextDirty = false;
    }

    private static void BindItem(Apparel item, Pawn? wearer)
    {
        List<Verb> verbs = new List<Verb>();
        List<Ability> abilities = item.AllAbilitiesForReading.ToList();
        foreach (IVerbOwner comp in item.AllComps.OfType<IVerbOwner>())
        {
            verbs.AddRange(comp.VerbTracker.AllVerbs);
        }

        if (item is IVerbOwner itemVerbOwner)
        {
            verbs.AddRange(itemVerbOwner.VerbTracker.AllVerbs);
        }

        foreach (Ability ability in abilities)
            verbs.AddRange(ability.VerbTracker.AllVerbs);

        if (verbs.Any(verb => verb.caster != null && verb.caster != wearer) ||
            abilities.Any(ability => ability.pawn != null && ability.pawn != wearer))
            ProxyGizmoUtility.Invalidate(verbs, abilities);

        foreach (Verb verb in verbs)
            BindVerb(verb, wearer);
        foreach (Ability ability in abilities)
        {
            ability.pawn = wearer;
        }
    }

    private static void BindVerb(Verb verb, Pawn? wearer)
    {
        if (verb.caster == wearer) return;
        // Detach the previous user's cast, but preserve verbs restored unbound during loading.
        if (verb.caster != null)
            verb.Reset();
        verb.caster = wearer;
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
                        if (InnerContainer[i].Destroyed) continue;
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
        RefreshProxyContext();
        base.CompTick();
        RefreshProxyContext();
        if (parent.Destroyed || boundWearer == null) return;

        var abilities = AllInnerAbilities;
        tickingAbilities = true;
        try
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                if (proxyContextDirty || Wearer != boundWearer) break;
                abilities[i].AbilityTick();
            }
        }
        finally
        {
            tickingAbilities = false;
            RefreshProxyContext();
        }
    }
}
