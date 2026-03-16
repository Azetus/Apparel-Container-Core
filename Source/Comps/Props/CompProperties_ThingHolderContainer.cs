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

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var err in base.ConfigErrors(parentDef))
        {
            yield return err;
        }

        if (storageCapacity <= 0)
        {
            yield return $"{parentDef.defName}: storageCapacity must be greater than 0. Current value: {storageCapacity}.";
        }
    }
}