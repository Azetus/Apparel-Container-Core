using ACC_ApparelContainerCore.Comps;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ACC_ApparelContainerCore.JobDrivers;

public class JobDriver_PutInGenericPackForApparel : JobDriver
{
    protected Thing? TargetThing => job.targetA.Thing;
    protected Thing? TargetPack => job.targetB.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        // 同时预留物品和包，防止包在走路过程中被脱掉
        return pawn.Reserve(TargetThing, job, 1, -1, null, errorOnFailed) &&
               pawn.Reserve(TargetPack, job, 1, -1, null, errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() =>
        {
            var targetAThing = TargetA.Thing;
            var targetBThing = TargetB.Thing;

            return targetBThing == null || targetBThing.Destroyed ||
                   !targetBThing.HasComp<Comp_GenericPackForApparel>() || targetAThing == null ||
                   targetAThing.Destroyed || targetAThing is not Apparel;
        });

        // 如果目标物品或者包毁了，停止
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        if (job.count <= 0)
            job.count = 1;

        if (TargetThing is not Apparel apparel || TargetPack is not ThingWithComps pack ||
            pack.TryGetComp<Comp_GenericPackForApparel>() is not Comp_GenericPackForApparel comp)
            yield break;

        Toil putInPack = new Toil();
        putInPack.initAction = delegate
        {
            Pawn actor = putInPack.actor;
            if (actor != null && apparel != null && comp != null)
            {
                int numToTake = Math.Min(job.count, apparel.stackCount);
                if (apparel.def.soundPickup != null)
                    apparel.def.soundPickup.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
                // 使用 SplitOff 剥离物体
                var thingToLoad = apparel.SplitOff(numToTake);

                // 尝试存入
                if (thingToLoad is Apparel apparelToLoad && comp.TryAdd(apparelToLoad, true))
                    Messages.Message($"loading {thingToLoad.LabelCap} into {pack.LabelCap}",
                        MessageTypeDefOf.PositiveEvent);
                else
                {
                    // 失败：将剥离出来的实例（thingToLoad）放回地面
                    GenPlace.TryPlaceThing(thingToLoad, actor.Position, actor.Map, ThingPlaceMode.Near);
                    Messages.Message("pack is full or unable to load item, dropping on the ground",
                        MessageTypeDefOf.NegativeEvent);
                }
            }
        };
        yield return putInPack;
    }
}