using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using ACC_ApparelContainerCore.DefOfs;
using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.FloatMenuOptions;

public class FloatMenuOptionProvider_GenericPackForApparel : FloatMenuOptionProvider_ThingHolderContainer<Apparel,
    CompProperties_GenericPackForApparel, Comp_GenericPackForApparel>
{
    protected override JobDef JobDef => ACC_JobDefOfs.ACC_Job_PutInGenericPackForApparel;

    protected override IEnumerable<Thing> GetContainersOnPawn(Pawn pawn)
    {
        return pawn.apparel?.WornApparel
            .Where(a => a.HasComp<Comp_GenericPackForApparel>()) ?? Enumerable.Empty<Thing>();
    }

    protected override bool CanLoadIntoTarget(Thing targetThing, Comp_GenericPackForApparel containerComp, Thing containerThing)
    {
        return containerComp.IsValidTargetToLoad(targetThing);
    }

    protected override bool IsValidTargetThing(Thing thing)
    {
        return Comp_GenericPackForApparel.IsValidTargetToLoadBase(thing);
    }
}