using Verse;

namespace ACC_ApparelContainerCore.Settings;

public static class SettingUtils
{
    public static bool IsInWhitelist(Def def)
    {
        return ApparelContainerCore.settings.whitelist.Contains(def.defName);
    }
    
    public static bool IsInBlacklist(Def def)
    {
        return ApparelContainerCore.settings.blacklist.Contains(def.defName);
    }

    public static bool IsUsingStrictWhitelistMode => ApparelContainerCore.settings.useStrictWhitelist;
}