using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.JobDrivers;

public abstract class JobDriver_ThingHolderContainer<TThing, TProps, TComp> : JobDriver
    where TThing : Thing
    where TProps : CompProperties_ThingHolderContainer
    where TComp : Comp_ThingHolderContainer<TThing, TProps>
{
    protected Thing? TargetThing => job.targetA.Thing;
    protected Thing? TargetContainer => job.targetB.Thing;
    protected TComp? ContainerComp => TargetContainer?.TryGetComp<TComp>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        // 同时预留物品和包，防止包在走路过程中被脱掉
        return pawn.Reserve(job.targetA.Thing, job, 1, -1, null, errorOnFailed) &&
               pawn.Reserve(job.targetB.Thing, job, 1, -1, null, errorOnFailed);
    }

    // 校验job的相关target和comp
    protected virtual bool ValidateTargets()
    {
        return TargetThing != null &&
               TargetContainer != null &&
               ContainerComp != null &&
               !TargetThing.Destroyed &&
               !TargetContainer.Destroyed &&
               TargetThing is TThing;
    }

    // 失败条件
    protected virtual void AddFailConditions()
    {
        this.FailOn(() => !ValidateTargets());
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
    }

    // 调用ACC_Comp的目标校验
    protected virtual bool CanLoadIntoContainer(TThing thing, TComp comp)
    {
        return comp.IsTargetInteractable(thing);
    }

    // 装载目标
    protected abstract int PerformLoad(TThing thing, TComp comp, int count);


    // 装载完成后执行的内容
    protected virtual void OnLoadComplete(int amountLoaded, TThing thing, Thing container)
    {
        if (amountLoaded > 0)
        {
            Messages.Message(
                "ACC_Message_LoadComplete".Translate(thing.LabelNoCount, amountLoaded, container.LabelCap),
                MessageTypeDefOf.PositiveEvent
            );
        }
        else
        {
            Messages.Message(
                "ACC_Message_LoadFail".Translate(thing.LabelNoCount,container.LabelCap),
                MessageTypeDefOf.RejectInput
            );
        }
    }


    public override IEnumerable<Toil> MakeNewToils()
    {
        // 失败条件
        AddFailConditions();

        // 走到物品位置
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        // 设置默认计数
        if (job.count <= 0)
            job.count = 1;

        // 创建装载物品的Toil
        Toil loadToil = new Toil
        {
            initAction = () =>
            {
                var thing = TargetThing;
                var comp = ContainerComp;

                if (thing is not TThing thingToLoad || comp is not TComp targetComp || !CanLoadIntoContainer(thingToLoad, targetComp))
                    return;

                int loaded = PerformLoad(thingToLoad, targetComp, job.count);
                OnLoadComplete(loaded, thingToLoad, TargetContainer!);
            }
        };

        yield return loadToil;
    }
}