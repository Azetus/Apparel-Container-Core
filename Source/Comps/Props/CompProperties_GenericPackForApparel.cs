using RimWorld;

namespace ACC_ApparelContainerCore.Comps.Props;

public class CompProperties_GenericPackForApparel : CompProperties_ThingHolderContainer
{
    protected override StatCategoryDef GetCompPropCapacityStatCategory => StatCategoryDefOf.Apparel;
    
    public CompProperties_GenericPackForApparel()
    {
        compClass = typeof(Comp_GenericPackForApparel);
    }
}