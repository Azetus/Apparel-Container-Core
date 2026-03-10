using Verse;

namespace ACC_ApparelContainerCore.Settings;

public class ApparelContainerCoreSetting: ModSettings
{
    // 白名单与黑名单
    public HashSet<string> whitelist = new HashSet<string>();
    public HashSet<string> blacklist = new HashSet<string>();

    // 严格模式
    public bool useStrictWhitelist = false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref useStrictWhitelist, "ACC_StrictWhitelist", false);

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            whitelist.RemoveWhere(x => x.NullOrEmpty());
            blacklist.RemoveWhere(x => x.NullOrEmpty());
        }
        
        Scribe_Collections.Look(ref whitelist, "ACC_Whitelist", LookMode.Value);
        Scribe_Collections.Look(ref blacklist, "ACC_Blacklist", LookMode.Value);
        
        if (whitelist == null) whitelist = new HashSet<string>();
        if (blacklist == null) blacklist = new HashSet<string>();
    }
    
    
    public void DoCleanup()
    {
        whitelist.Clear();
        blacklist.Clear();
        this.Write();
    }
}