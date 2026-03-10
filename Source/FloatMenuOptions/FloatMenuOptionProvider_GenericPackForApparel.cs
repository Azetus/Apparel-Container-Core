using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.DefOfs;
using RimWorld;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.FloatMenuOptions;

public class FloatMenuOptionProvider_GenericPackForApparel : FloatMenuOptionProvider
{
    public override bool Drafted => true;
    public override bool Undrafted => true;
    public override bool Multiselect => false;
    public override bool MechanoidCanDo => true;
    public override bool RequiresManipulation => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
    {
        if (!base.SelectedPawnValid(pawn, context))
            return false;
        return pawn.apparel?.WornApparel
            .Any(a => a.HasComp<Comp_GenericPackForApparel>()) ?? false;
    }

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        if (!base.TargetThingValid(thing, context))
            return false;
        // if(context.FirstSelectedPawn is Pawn pawn && thing.IsForbidden(pawn))
        //     return false;
        return Comp_GenericPackForApparel.IsValidTargetToLoadBase(thing);
    }

    // 返回右键菜单选项
    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        if (pawn == null || clickedThing == null) yield break;
        var containers = pawn.apparel?.WornApparel
            .Where(a => a.HasComp<Comp_GenericPackForApparel>())
            .ToList();

        if (containers == null || containers.NullOrEmpty()) yield break;
        // 为每一个容器生成一个独立的 FloatMenuOption
        foreach (var container in containers)
        {
            if (container == null) continue;
            var comp = container.TryGetComp<Comp_GenericPackForApparel>();
            if (comp == null) continue;

            if (comp.CanAcceptMore && comp.IsValidTargetToLoad(clickedThing))
            {
                string label = $"Loading into {container.Label}";
                yield return new FloatMenuOption(label, () =>
                {
                    clickedThing.SetForbidden(value: false);
                    Job job = JobMaker.MakeJob(ACC_JobDefOfs.ACC_Job_PutInGenericPackForApparel, clickedThing, container);
                    job.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }
    }
}