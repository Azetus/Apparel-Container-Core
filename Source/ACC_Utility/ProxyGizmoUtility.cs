using RimWorld;
using Verse;

namespace ACC_ApparelContainerCore.ACC_Utility;

internal static class ProxyGizmoUtility
{
    public static void Invalidate(List<Verb> verbs, List<Ability> abilities)
    {
        // The native UI keeps the hovered command after its source item changes owner.
        if (Matches(MapGizmoUtility.mouseoverGizmo, verbs, abilities))
            MapGizmoUtility.mouseoverGizmo = null;
        if (Matches(MapGizmoUtility.lastMouseOverGizmo, verbs, abilities))
            MapGizmoUtility.lastMouseOverGizmo = null;

        // Rebuild on the next GUI pass, including another pass within the same frame.
        // Do not modify command lists that the current GUI pass may still be iterating.
        GizmoGridDrawer.cacheFrame = -1;

        Targeter? targeter = (Current.Root?.uiRoot as UIRoot_Play)?.mapUI?.targeter;
        if (targeter == null) return;
        if ((targeter.targetingSource?.GetVerb is Verb source && verbs.Contains(source)) ||
            (targeter.targetingSourceParent?.GetVerb is Verb parent && verbs.Contains(parent)))
        {
            targeter.StopTargeting();
            targeter.targetingSourceParent = null;
            targeter.targetingSourceAdditionalPawns?.Clear();
        }
    }

    private static bool Matches(Gizmo? gizmo, List<Verb> verbs, List<Ability> abilities)
    {
        return (gizmo is Command_VerbTarget command && verbs.Contains(command.verb)) ||
               (gizmo is Command_Ability abilityCommand && abilities.Contains(abilityCommand.Ability));
    }
}
