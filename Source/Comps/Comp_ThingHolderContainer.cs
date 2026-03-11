using System.Text;
using ACC_ApparelContainerCore.Comps.Props;
using ACC_ApparelContainerCore.Dialog;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ACC_ApparelContainerCore.ACC_Utility.UtilityChecker;
using static ACC_ApparelContainerCore.ACC_Utility.ACC_IconTexture;

namespace ACC_ApparelContainerCore.Comps;

public abstract class Comp_ThingHolderContainer<T, TP> : Comp_ACC_ThingHolderContainerBase, IThingHolder
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


    public virtual void Notify_InnerContainerContentsChanged()
    {
    }

    public override void PostPostMake()
    {
        base.PostPostMake();
        if (_innerContainer == null)
            _innerContainer = new ThingOwner<T>(this, false, LookMode.Deep);
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
        bool dropRes = _innerContainer.TryDrop(thing, dropLoc, map, mode, out lastResultingThing, placedAction,
            nearPlaceValidator);
        if (dropRes)
            Notify_InnerContainerContentsChanged();
        return dropRes;
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
        bool dropRes = _innerContainer.TryDrop(thing, dropLoc, map, mode, count, out resultingThing, placedAction,
            nearPlaceValidator);
        if (dropRes)
            Notify_InnerContainerContentsChanged();
        return dropRes;
    }

    public int TryLoadInto(T item, int count, bool canMergeWithExistingStacks = false)
    {
        if (item == null || count <= 0)
            return 0;
        if (!CanAcceptMore)
            return 0;
        if (!IsTargetInteractable(item))
            return 0;
        int numToTake = Mathf.Min(count, item.stackCount);
        Thing thingToLoad = item.SplitOff(numToTake);
        int actuallyAdded = _innerContainer.TryAdd(thingToLoad, numToTake, canMergeWithExistingStacks);

        if (actuallyAdded > 0)
        {
            Notify_InnerContainerContentsChanged();
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

    public bool TryDropAll(Map map)
    {
        // 如果在地图上，则把东西全部吐出来
        if (map != null)
        {
            IntVec3 dropPos = Wearer?.PositionHeld ?? parent.PositionHeld;
            if (_innerContainer != null)
            {
                bool dropAllRes = _innerContainer.TryDropAll(dropPos, map, ThingPlaceMode.Near);
                if (dropAllRes)
                    Notify_InnerContainerContentsChanged();
                return dropAllRes;
            }
        }

        return false;
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (_innerContainer == null) return;

        if (previousMap != null)
            TryDropAll(previousMap);
        else
            _innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
        Notify_InnerContainerContentsChanged();
    }

    /// <summary>
    /// 调用一下 IsForbidden
    /// </summary>
    public virtual bool IsTargetInteractable(Thing thingToInteract)
    {
        if (thingToInteract.IsForbidden(Wearer)) return false;
        return IsValidTargetToLoad(thingToInteract);
    }

    /// <summary>
    /// 判断一个物品是否可以进入容器
    /// </summary>
    /// <remarks>
    /// Checks if the item meets the requirements to enter the container.
    /// </remarks>
    public virtual bool IsValidTargetToLoad(Thing thingToLoad)
    {
        if (!IsValidTargetToLoadBase(thingToLoad)) return false;
        // 不许套娃
        if (thingToLoad == this.parent) return false;
        return IsFunctionalUtility<T>(thingToLoad);
    }

    /// <summary>
    /// 这里只进行最基础的判断，过滤功能性装备的校验交给子类实现。 
    /// 此外 IsForbidden(this Thing t, Pawn pawn) 对于征召状态的Pawn有特殊处理，是否允许交互被禁用的物品最好视情况判断
    /// </summary>
    /// <remarks>
    /// Performs only the most fundamental checks. Validation for functional equipment is deferred to subclasses.
    /// Additionally, IsForbidden(this Thing t, Pawn pawn) handles drafted Pawns uniquely; 
    /// whether to allow interaction with forbidden items should be evaluated by the caller based on the specific context.
    /// </remarks>
    public static bool IsValidTargetToLoadBase(Thing thingToLoad)
    {
        // 基础类别过滤：必须是 Item 类别
        if (thingToLoad.def.category != ThingCategory.Item) return false;
        // 状态检查：必须在地图上，且未被销毁
        if (!thingToLoad.Spawned || thingToLoad.Destroyed) return false;
        // 是否在燃烧
        if (thingToLoad.IsBurning()) return false;
        // 因为在调用comp时设置了Owner为对应Tracker，item的类型必须是V
        if (thingToLoad is not T) return false;
        // 不许套娃
        if (thingToLoad is IThingHolder) return false;
        if (thingToLoad is ThingWithComps thingWithCompsToLoad &&
            thingWithCompsToLoad.AllComps.Any(c => c is IThingHolder)) return false;
        return true;
    }

    // --- 处理Gizmo ---
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var g in base.CompGetWornGizmosExtra()) yield return g;
        if (AnyItem)
        {
            yield return CreateManagementGizmo();
            yield return GetDropAllGizmo();
        }
    }

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        foreach (var g in base.CompGetWornGizmosExtra()) yield return g;
        foreach (var containerGizmos in GetContainerGizmos()) yield return containerGizmos;
        foreach (var extraGizmo in GetExtraGizmosInContainer()) yield return extraGizmo;
    }

    protected virtual IEnumerable<Gizmo> GetContainerGizmos()
    {
        yield return CreateManagementGizmo();
    }

    // ----- 代理容器内物品的 Gizmo -----
    protected abstract IEnumerable<Gizmo> GetExtraGizmosInContainer();

    // ----- 打开背包管理窗口 -----
    protected virtual Gizmo CreateManagementGizmo()
    {
        return new Command_Action
        {
            defaultLabel = parent.def.label,
            defaultDesc = "ACC_ManagePackGizmo_defaultDesc".Translate(),
            icon = parent.def.uiIcon,
            action = () =>
            {
                if (parent.MapHeld == null) return;
                var window = new Dialog_ContainerManagement<T, TP>(this);
                Find.WindowStack.Add(window);
            }
        };
    }

    // ----- 清空背包的Gizmo -----
    protected Gizmo GetDropAllGizmo()
    {
        return new Command_Action
        {
            defaultLabel = "ACC_DropAll_label".Translate(),
            defaultDesc = "ACC_DropAll_Desc".Translate(),
            icon = VMM_DropAll_Icon,
            action = () =>
            {
                if (parent.MapHeld is Map curMap)
                    TryDropAll(curMap);
            }
        };
    }

    /// <summary>
    /// 注意：在使用此方法进行身份伪装 (owner Proxy) 时，必须确保此容器的泛型类型 <typeparamref name="T"/> 
    /// 与目标持有者 (newTarget) 内部预期的容器类型完全匹配。
    /// 例如：若 newTarget 为 Pawn_ApparelTracker，则 T 必须派生自 Apparel。
    /// 类型不匹配会导致 Pawn_ApparelTracker 在处理物品移除 (NotifyRemoved) 时触发 InvalidCastException。
    /// </summary>
    /// <remarks>
    /// Note: When using this method for owner proxying, the generic type <typeparamref name="T"/> 
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
    /// 调用此方法之前需要对 ownerItem 调用 SetOwner
    /// Rimworld 会对 Label 等字段相同的 Gizmo 进行合并，必须对 Label 加以修改，否则装载多个同类物品时只会显示一个 Gizmo
    /// NOTE：在此处需要对 "Verb.caster" 重新赋值，让容器内物品的 Verb.caster 指向装备者
    /// </summary>
    /// <remarks>
    /// SetOwner must be called on ownerItem before invoking this method.
    /// Since RimWorld merges Gizmos with identical Labels, the Label must be modified; 
    /// otherwise, only one Gizmo will be displayed when multiple items of the same type are equipped.
    /// NOTE: "Verb.caster" needs to be reassigned here to ensure that the Verb.caster of the 
    /// contained item correctly points to the equipper.
    /// </remarks>
    protected virtual Gizmo ProcessProxyGizmo(Gizmo gizmo,  int index)
    {
        // 仅处理命令类 Gizmo (Command)
        if (gizmo is Command command)
        {
            // 区分 Label，防止 UI 合并图标
            command.defaultLabel += $"[{index}]";
            command.defaultDesc = parent.LabelShort + "\n\n" + command.defaultDesc;
            command.groupable = false;
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


    public override string CompInspectStringExtra()
    {
        string baseString = base.CompInspectStringExtra();
        StringBuilder strBuilder = new StringBuilder();

        if (!baseString.NullOrEmpty())
        {
            strBuilder.AppendLine(baseString);
        }

        strBuilder.Append("ACC_Capacity_label".Translate() + ": " + ContainerCount + " / " + Props.storageCapacity);

        if (ContainerCount > 0)
        {
            strBuilder.AppendLine();
            strBuilder.Append("ACC_Contents_label".Translate() + ": ");
            List<string> itemLabels = new List<string>();
            for (int i = 0; i < ContainerCount; i++)
            {
                itemLabels.Add(ContainedThings[i].LabelShort);
            }

            strBuilder.Append(string.Join(", ", itemLabels));
        }

        return strBuilder.ToString().TrimEnd();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref _innerContainer, "ACC_InnerContainer", this);
    }
}