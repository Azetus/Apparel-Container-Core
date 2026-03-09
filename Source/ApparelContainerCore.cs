using ACC_ApparelContainerCore.Settings;
using ACC_ApparelContainerCore.Settings.SettingUI;
using UnityEngine;
using Verse;

namespace ACC_ApparelContainerCore;

public class ApparelContainerCore : Mod
{
    public static ApparelContainerCoreSetting settings;

    public ApparelContainerCore(ModContentPack contentPack) : base(contentPack)
    {
        settings = GetSettings<ApparelContainerCoreSetting>();

        Log.Message("<color=cyan>[ApparelContainerCore]</color> is loaded!");
    }

    public override string SettingsCategory()
    {
        return "Apparel Container Core";
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        ACC_SettingsWindowContents.DoSettingsWindowContents(inRect, ref settings);
    }
}