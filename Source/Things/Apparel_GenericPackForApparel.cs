using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.Things;

public class Apparel_GenericPackForApparel:Apparel_ThingHolderContainer<Apparel>
{
    protected override IEnumerable<Gizmo> GetContainerGizmos()
    {
        throw new NotImplementedException();
    }

    protected override IEnumerable<Gizmo> GetExtraGizmos()
    {
        throw new NotImplementedException();
    }
}