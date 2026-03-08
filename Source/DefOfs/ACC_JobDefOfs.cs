using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.DefOfs;

[DefOf]
public class ACC_JobDefOfs
{
    public static JobDef ACC_Job_PutInGenericPackForApparel;

    static ACC_JobDefOfs()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ACC_JobDefOfs));
    }
}