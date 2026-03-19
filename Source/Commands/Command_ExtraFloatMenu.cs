using UnityEngine;
using Verse;

namespace ACC_ApparelContainerCore.Commands;

public class Command_ExtraFloatMenu : Command_Action
{
    public Func<List<FloatMenuOption>>? floatMenuOptions;

    public override void ProcessInput(Event ev)
    {
        // 左键点击
        if (ev.button == 0)
        {
            base.ProcessInput(ev);
        }
        // 右键点击
        else if (ev.button == 1)
        {
            if (floatMenuOptions != null)
            {
                List<FloatMenuOption>? options = floatMenuOptions();
                if (!options.NullOrEmpty())
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
        }
    }
}