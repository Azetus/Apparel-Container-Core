using Verse;

namespace ACC_ApparelContainerCore.Comps;

/// <summary>
/// Keeps the proxy owner for wearer queries, but sends storage notifications to the real container.
/// The inherited save data is unchanged, so existing ACC_InnerContainer nodes remain readable.
/// </summary>
public class ContainerThingOwner<T> : ThingOwner<T> where T : Thing
{
    private readonly IThingHolder containerOwner;
    private IThingHolder proxyOwner;
    private int notificationDepth;

    public ContainerThingOwner(IThingHolder owner) : base(owner, false, LookMode.Deep)
    {
        containerOwner = owner;
        proxyOwner = owner;
    }

    public void SetProxyOwner(IThingHolder target)
    {
        proxyOwner = target;
        if (notificationDepth == 0)
            owner = target;
    }

    protected override void NotifyAdded(Thing item)
    {
        notificationDepth++;
        owner = containerOwner;
        try
        {
            base.NotifyAdded(item);
        }
        finally
        {
            notificationDepth--;
            owner = notificationDepth == 0 ? proxyOwner : containerOwner;
        }
    }

    protected override void NotifyRemoved(Thing item)
    {
        notificationDepth++;
        owner = containerOwner;
        try
        {
            base.NotifyRemoved(item);
        }
        finally
        {
            notificationDepth--;
            owner = notificationDepth == 0 ? proxyOwner : containerOwner;
        }
    }
}
