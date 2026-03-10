using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;


namespace ACC_ApparelContainerCore.JobDrivers;

public class JobDriver_PutInGenericPackForApparel : JobDriver_ThingHolderContainer<Apparel, CompProperties_GenericPackForApparel,
    Comp_GenericPackForApparel>
{
    protected override int PerformLoad(Apparel thing, Comp_GenericPackForApparel comp, int count)
    {
        return comp.TryLoadInto(thing, job.count);
    }
}