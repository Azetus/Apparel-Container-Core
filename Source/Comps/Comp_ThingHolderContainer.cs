using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ACC_ApparelContainerCore.Comps;

public abstract class Comp_ThingHolderContainer<T, TP> : ThingComp, IThingHolder
    where T : Thing
    where TP : CompProperties_ThingHolderContainer
{
    private ThingOwner<T> _innerContainer;
    public TP Props => (TP)this.props;

    protected List<T> InnerContainer => _innerContainer.InnerListForReading;

    public IReadOnlyList<T> ContainedThings => _innerContainer.InnerListForReading;

    public int ContainerCount => _innerContainer.Count;

    public bool AnyItem => _innerContainer.Count != 0;

    public bool CanAcceptMore => ContainerCount < Props.storageCapacity;


    public Pawn? Wearer => (base.ParentHolder as Pawn_ApparelTracker)?.pawn;

    public override void PostPostMake()
    {
        base.PostPostMake();
        if (_innerContainer == null)
        {
            _innerContainer = new ThingOwner<T>(this, false, LookMode.Deep);
        }
    }

    public ThingOwner GetDirectlyHeldThings() => _innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public bool TryDrop(
        T thing,
        IntVec3 dropLoc,
        Map map,
        ThingPlaceMode mode,
        out T lastResultingThing,
        Action<T, int>? placedAction = null,
        Predicate<IntVec3>? nearPlaceValidator = null)
    {
        return _innerContainer.TryDrop(thing, dropLoc, map, mode, out lastResultingThing, placedAction,
            nearPlaceValidator);
    }

    public bool TryDrop(
        Thing thing,
        IntVec3 dropLoc,
        Map map,
        ThingPlaceMode mode,
        int count,
        out T resultingThing,
        Action<T, int> placedAction = null,
        Predicate<IntVec3> nearPlaceValidator = null)
    {
        return _innerContainer.TryDrop(thing, dropLoc, map, mode, count, out resultingThing, placedAction,
            nearPlaceValidator);
    }

    public int TryLoadInto(T item, int count, bool canMergeWithExistingStacks = false)
    {
        if (item == null || count <= 0)
            return 0;
        if (!CanAcceptMore)
            return 0;
        int numToTake = Mathf.Min(count, item.stackCount);
        Thing thingToLoad = item.SplitOff(numToTake);
        int actuallyAdded = _innerContainer.TryAdd(thingToLoad, numToTake, canMergeWithExistingStacks);

        if (actuallyAdded > 0)
        {
            if (thingToLoad.def.soundPickup != null && Wearer != null)
                thingToLoad.def.soundPickup.PlayOneShot(new TargetInfo(Wearer.Position, Wearer.Map));
            Wearer?.MapHeld?.resourceCounter.UpdateResourceCounts();
            return actuallyAdded;
        }
        else
        {
            if (thingToLoad != null && Wearer != null && !thingToLoad.Destroyed)
                GenPlace.TryPlaceThing(thingToLoad, Wearer.Position, Wearer.Map, ThingPlaceMode.Near);
            return 0;
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        // 如果在地图上，则把东西全部吐出来
        if (previousMap != null)
        {
            IntVec3 dropPos = Wearer?.PositionHeld ?? parent.PositionHeld;
            // 倒序遍历移除物品
            for (int i = InnerContainer.Count - 1; i >= 0; i--)
            {
                if (InnerContainer[i] is { } thing)
                    TryDrop(InnerContainer[i], dropPos, previousMap, ThingPlaceMode.Near, out _);
            }
        }

        base.PostDestroy(mode, previousMap);
    }

    /// <summary>
    /// 核心过滤逻辑：判断一个物品是否符合容器交互要求
    /// </summary>
    protected virtual bool IsValidTargetToLoad(Thing thingToLoad)
    {
        // 基础类别过滤：必须是 Item 类别
        if (thingToLoad.def.category != ThingCategory.Item) return false;
        // 状态检查：必须在地图上，且未被销毁
        if (!thingToLoad.Spawned || thingToLoad.Destroyed) return false;
        // 权限检查：是否被禁用了，或者正在被其他人交互
        if (thingToLoad.IsForbidden(Wearer) || thingToLoad.IsBurning()) return false;
        // 因为在调用comp时设置了Owner为对应Tracker，item的类型必须是T
        if (thingToLoad is not T) return false;
        // TODO (预留) 白名单/黑名单逻辑

        // 不许套娃
        if (thingToLoad == this.parent) return false;
        if (thingToLoad is IThingHolder) return false;
        if (thingToLoad is ThingWithComps thingWithCompsToLoad &&
            thingWithCompsToLoad.AllComps.Any(c => c is IThingHolder)) return false;
        return true;
    }

    // --- 处理Gizmo ---
    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        foreach (var g in base.CompGetWornGizmosExtra()) yield return g;
        foreach (var containerGizmos in GetContainerGizmos()) yield return containerGizmos;
        foreach (var extraGizmo in GetExtraGizmosInContainer()) yield return extraGizmo;
    }

    protected abstract IEnumerable<Gizmo> GetContainerGizmos();

    // ----- 代理容器内物品的 Gizmo -----
    protected abstract IEnumerable<Gizmo> GetExtraGizmosInContainer();

    /// <summary>
    /// 注意：RimWorld 的 IThingHolder 接口并非类型安全。
    /// 警告：在使用此方法进行身份伪装 (Identity Proxy) 时，必须确保此容器的泛型类型 <typeparamref name="T"/> 
    /// 与目标持有者 (newTarget) 内部预期的容器类型完全匹配。
    /// 例如：若 newTarget 为 Pawn_ApparelTracker，则 T 必须派生自 Apparel。
    /// 类型不匹配会导致 Pawn_ApparelTracker 在处理物品移除 (NotifyRemoved) 时触发 InvalidCastException。
    /// </summary>
    /// <remarks>
    /// Note: RimWorld's IThingHolder interface is NOT type-safe as it lacks content type information.
    /// WARNING: When using this method for identity proxying, the generic type <typeparamref name="T"/> 
    /// MUST be strictly compatible with the underlying collection type expected by the 'newTarget'.
    /// Example: If 'newTarget' is a Pawn_ApparelTracker, T MUST derive from Apparel.
    /// Mismatched types will trigger an InvalidCastException during the target's internal 
    /// notification logic (e.g., NotifyRemoved) when an item is dropped or destroyed.
    /// </remarks>
    protected virtual void SetOwner(ThingOwner owner, IThingHolder newTarget)
    {
        // 将 ThingOwner 里的 owner 字段设置为 newTarget
        owner.owner = newTarget;
    }


    /// <summary>
    /// Rimworld 会对 Label 等字段相同的 Gizmo 进行合并，必须对 Label 加以修改，否则装载多个同类物品时只会显示一个 Gizmo
    /// 在此处需要对 "Verb.caster" 重新赋值，让容器内物品的 Verb.caster 指向装备者
    /// </summary>
    /// <remarks>
    /// RimWorld merges Gizmos that share identical fields such as Label.
    /// Therefore, the Label must be modified; otherwise, when multiple identical items are loaded,
    /// only a single Gizmo will be displayed.
    /// Here we also reassign "Verb.caster" so that the Verb of items inside the container
    /// uses the wearer of the apparel as the caster.
    /// </remarks>
    protected virtual Gizmo ProcessProxyGizmo(Gizmo gizmo, ThingWithComps ownerItem, int index)
    {
        // 仅处理命令类 Gizmo (Command)
        if (gizmo is Command command)
        {
            // 区分 Label，防止 UI 合并图标
            command.defaultLabel = $"{ownerItem.LabelCapNoCount} [{index}]";
            // 核心：处理 Verb.caster关联
            if (command is Command_VerbTarget { verb: not null } verbCommand)
                verbCommand.verb.caster = this.Wearer;
        }

        return gizmo;
    }

    /**
     * 屏蔽部分Gizmo
     */
    protected virtual bool ShouldShowGizmo(Gizmo gizmo)
    {
        // 核心规则：只显示"Command"按钮。理论上这会自动剔除掉能量条、耐久条等绘图类 Gizmo
        if (!(gizmo is Command cmd))
            return false;
        // 排除掉物品自带的 允许/禁用 按钮
        if (cmd.hotKey == KeyBindingDefOf.Command_ItemForbid && cmd.icon == TexCommand.ForbidOff)
            return false;
        return true;
    }


    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref _innerContainer, "ACC_InnerContainer", this);
    }
}