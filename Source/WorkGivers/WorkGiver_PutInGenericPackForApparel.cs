using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.DefOfs;
using RimWorld;
using Verse;
using Verse.AI;

namespace ACC_ApparelContainerCore.WorkGivers;

public class WorkGiver_PutInGenericPackForApparel : WorkGiver_Scanner
{
    // 扫描目标 Apparel
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Apparel);

    // 移动到目标的距离（Touch）
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    // 是否应该显示右键菜单
    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!forced) return false;
        if (!Comp_GenericPackForApparel.IsValidTargetToLoadBase(t)) return false;
        var container = pawn.apparel?.WornApparel
            .Select(a => a.TryGetComp<Comp_GenericPackForApparel>())
            .FirstOrDefault(c => c != null);
        if (container == null) return false;
        // 验证小人是否有能力到达并操作
        if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly)) return false;
        // 容器是否已满
        return container.CanAcceptMore;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var container = pawn.apparel?.WornApparel
            .FirstOrDefault(a => a.HasComp<Comp_GenericPackForApparel>());
        if (container != null)
        {
            Job job = JobMaker.MakeJob(ACC_JobDefOfs.ACC_Job_PutInGenericPackForApparel, t, container);
            job.count = 1;
            return job;
        }
        return null;
    }
}