using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.FloatMenuOptions;

public abstract class FloatMenuOptionProvider_ThingHolderContainer<TThing, TProps, TComp> : FloatMenuOptionProvider
    where TThing : Thing
    where TProps : CompProperties_ThingHolderContainer
    where TComp : Comp_ThingHolderContainer<TThing, TProps>
{
    public override bool Drafted => true;
    public override bool Undrafted => true;
    public override bool Multiselect => false;
    public override bool MechanoidCanDo => true;
    public override bool RequiresManipulation => true;

    protected abstract JobDef JobDef { get; }

    protected abstract IEnumerable<Thing> GetContainersOnPawn(Pawn pawn);

    protected abstract bool CanLoadIntoTarget(Thing targetThing, TComp containerComp, Thing containerThing);

    protected abstract bool IsValidTargetThing(Thing thing);

    protected virtual string GetMenuLabel(Thing container) => "ACC_FloatMenu_LoadingTargetInto_label".Translate(container.Label);

    protected virtual Job CreateJob(Thing targetThing, Thing container, int count)
    {
        Job job = JobMaker.MakeJob(JobDef, targetThing, container);
        job.count = count;
        return job;
    }

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
    {
        if (!base.SelectedPawnValid(pawn, context))
            return false;
        return GetContainersOnPawn(pawn).Any();
    }

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        if (!base.TargetThingValid(thing, context))
            return false;
        return IsValidTargetThing(thing);
    }

    // 返回右键菜单选项
    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        if (pawn == null || clickedThing == null) yield break;
        var containers = GetContainersOnPawn(pawn).ToList();
        if (containers.NullOrEmpty())
            yield break;
        foreach (var container in containers)
        {
            if (container == null)
                continue;
            var comp = container.TryGetComp<TComp>();
            if (comp == null)
                continue;

            if (comp.CanAcceptMore && CanLoadIntoTarget(clickedThing, comp, container))
            {
                string label = GetMenuLabel(container);
                yield return new FloatMenuOption(label, () =>
                {
                    clickedThing.SetForbidden(false);
                    Job job = CreateJob(clickedThing, container, 1);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }
    }
}