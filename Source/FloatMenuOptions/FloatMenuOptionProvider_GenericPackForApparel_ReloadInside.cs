using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using RimWorld.Utility;
using Verse;

namespace ACC_ApparelContainerCore.FloatMenuOptions;

public class FloatMenuOptionProvider_GenericPackForApparel_ReloadInside: FloatMenuOptionProvider_ReloadInsideContainer<Apparel,
    CompProperties_GenericPackForApparel, Comp_GenericPackForApparel>
{
    protected override IEnumerable<IReloadableComp> GetReloadablesUsingAmmoInsideContainer(Pawn pawn, Thing clickedThing)
    {
        if (pawn.apparel == null) yield break;

        // 检测身上穿着的服装
        foreach (Apparel item in pawn.apparel.WornApparel)
        {
            // 检测服装是否作存在 Comp_ThingHolderContainer ，并扫描其内部物品
            var containerComp = item.TryGetComp<Comp_GenericPackForApparel>();
            if (containerComp != null)
            {
                foreach (Thing innerThing in containerComp.GetDirectlyHeldThings())
                {
                    if (innerThing is ThingWithComps innerWithComps)
                    {
                        IReloadableComp? innerRel = innerWithComps.TryGetComp<CompApparelReloadable>();
                        if (innerRel != null && clickedThing.def == innerRel.AmmoDef)
                        {
                            yield return innerRel;
                        }
                    }
                }
            }
        }
    }
}