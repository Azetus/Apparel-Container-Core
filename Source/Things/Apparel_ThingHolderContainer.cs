using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.Things;

public abstract class Apparel_ThingHolderContainer<T> : Apparel, IThingHolder where T : Thing
{
    private ThingOwner<T> InnerContainer;

    public override void PostMake()
    {
        base.PostMake();
        if (InnerContainer == null)
        {
            InnerContainer = new ThingOwner<T>(this, false, LookMode.Deep);
        }
    }

    public ThingOwner GetDirectlyHeldThings() => InnerContainer;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref InnerContainer, "InnerContainer", this);
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        // 如果在地图上，则把东西全部吐出来
        if (this.MapHeld != null)
        {
            // 倒序遍历移除物品
            for (int i = InnerContainer.Count - 1; i >= 0; i--)
            {
                InnerContainer.TryDrop(InnerContainer[i], PositionHeld, MapHeld, ThingPlaceMode.Near, out var outThing);
            }
        }

        base.Destroy(mode);
    }

    public override IEnumerable<Gizmo> GetWornGizmos()
    {
        foreach (var g in base.GetWornGizmos()) yield return g;
        foreach (var containerGizmos in GetContainerGizmos()) yield return containerGizmos;
        foreach (var extraGizmo in GetExtraGizmos()) yield return extraGizmo;
    }

    protected abstract IEnumerable<Gizmo> GetContainerGizmos();
    
    // ----- 代理容器内物品的 Gizmo -----
    protected abstract IEnumerable<Gizmo> GetExtraGizmos();

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
}