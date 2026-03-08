using ACC_ApparelContainerCore.Things;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ACC_ApparelContainerCore.JobDrivers;

public class JobDriver_PutInGenericPackForApparel : JobDriver
{
    protected Thing TargetThing => job.targetA.Thing;
    protected Apparel_GenericPackForApparel TargetPack => job.targetB.Thing as Apparel_GenericPackForApparel;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        // 同时预留物品和包，防止包在走路过程中被脱掉
        return pawn.Reserve(TargetThing, job, 1, -1, null, errorOnFailed) &&
               pawn.Reserve(TargetPack, job, 1, -1, null, errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        // 如果目标物品或者包毁了，停止
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        if (job.count <= 0)
            job.count = 1;
        
        Toil putInPack = new Toil();
        // TODO: 捡东西的时候需要再验证一下类型，不是所有东西都能放进背包
        putInPack.initAction = delegate
        {
            Pawn actor = putInPack.actor;
            Thing thing = TargetThing; // Job.targetA
            Apparel_GenericPackForApparel pack = TargetPack; // Job.targetB
            if (actor != null && thing != null)
            {
                int numToTake = Math.Min(job.count, thing.stackCount);
                if (thing.def.soundPickup != null)
                    thing.def.soundPickup.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
                // 使用 SplitOff 剥离物体
                Thing thingToLoad = thing.SplitOff(numToTake);

                // 2. 尝试存入
                if (pack.GetDirectlyHeldThings().TryAdd(thingToLoad, true))
                    Messages.Message($"loading {thingToLoad.LabelCap} into {pack.LabelCap}", MessageTypeDefOf.PositiveEvent);
                else
                {
                    // 失败：将剥离出来的实例（thingToLoad）放回地面
                    GenPlace.TryPlaceThing(thingToLoad, actor.Position, actor.Map, ThingPlaceMode.Near);
                    Messages.Message("pack is full or unable to load item, dropping on the ground", MessageTypeDefOf.NegativeEvent);
                }
            }
        };
        yield return putInPack;
    }
}