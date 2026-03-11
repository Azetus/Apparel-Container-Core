using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.Comps.Props;

public abstract class CompProperties_ThingHolderContainer : CompProperties
{
    public int storageCapacity = 3;
    
    protected virtual StatCategoryDef GetCompPropCapacityStatCategory => StatCategoryDefOf.Apparel;

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats(req);
        if (enumerable != null)
        {
            foreach (var item in enumerable)
                if (item != null)
                    yield return item;
        }

        yield return new StatDrawEntry(
            GetCompPropCapacityStatCategory,
            "ACC_Stats_Capacity_label".Translate(),
            storageCapacity.ToString(),
            "ACC_Stats_Capacity_desc".Translate(),
            0
        );
    }
}