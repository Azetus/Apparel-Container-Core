using RimWorld;
using UnityEngine;
using Verse;

namespace ACC_ApparelContainerCore.Dialog;


public class Dialog_ItemSelect : Window
{
    private readonly List<Thing> originalMapItems;      // 初始地图物品
    private readonly List<Thing> originalContainerItems; // 初始包内物品
    
    // 状态追踪
    private readonly HashSet<Thing> toLoad = new HashSet<Thing>(); 
    private readonly HashSet<Thing> toUnload = new HashSet<Thing>(); 

    private readonly Action<List<Thing>, List<Thing>> onConfirmed;
    private string searchTerm = "";
    private Vector2 scrollPosition = Vector2.zero;
    private readonly int maxCapacity;

    public override Vector2 InitialSize => new Vector2(500f, 750f);

    public Dialog_ItemSelect(IEnumerable<Thing> mapItems, IEnumerable<Thing> containerItems, int capacity, Action<List<Thing>, List<Thing>> onConfirmed)
    {
        this.originalMapItems = mapItems.ToList();
        this.originalContainerItems = containerItems.ToList();
        this.maxCapacity = capacity;
        this.onConfirmed = onConfirmed;
        this.doCloseX = true;
        this.closeOnClickedOutside = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Block 1：原本就在包里的（扣除计划卸载的）+ 计划从地图装载的
        var bagDisplay = originalContainerItems.Except(toUnload).Concat(toLoad)
            .Where(t => t.Label.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        // Block 2：原本在地图上的（扣除计划装载的）+ 计划从包里卸载的
        var mapDisplay = originalMapItems.Except(toLoad).Concat(toUnload)
            .Where(t => t.Label.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        int projectedCount = bagDisplay.Count;

        // --- 头部区域 ---
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Container");
        
        Text.Font = GameFont.Small;
        GUI.color = projectedCount > maxCapacity ? ColorLibrary.RedReadable : Color.white;
        Widgets.Label(new Rect(0f, 35f, inRect.width, 25f), $"Capacity: {projectedCount} / {maxCapacity}");
        GUI.color = Color.white;

        searchTerm = Widgets.TextField(new Rect(0f, 65f, inRect.width, 30f), searchTerm);

        // --- 滚动列表区域 ---
        Rect outRect = new Rect(0f, 105f, inRect.width, inRect.height - 180f);
        // 总高度：两个 Header (30px * 2) + 物品行 (35px * 总数)
        float totalHeight = 60f + (bagDisplay.Count + mapDisplay.Count) * 35f;
        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, totalHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        float curY = 0f;

        // BLOCK 1: 已经在包内 / 计划装入
        DrawSectionHeader(ref curY, viewRect.width, "Load to container");
        foreach (var t in bagDisplay)
        {
            // 在包内的物品点击后标记为“卸载”
            bool isNewAdd = toLoad.Contains(t);
            DrawRow(new Rect(0f, curY, viewRect.width, 32f), t, "\u25BC", isNewAdd ? Color.green : Color.white, () => {
                if (isNewAdd) toLoad.Remove(t); // 如果是新加的，撤销增加
                else toUnload.Add(t);          // 如果是原有的，标记卸载
            });
            curY += 35f;
        }

        curY += 10f; 

        // BLOCK 2: 在地图上 / 计划卸载
        DrawSectionHeader(ref curY, viewRect.width, "Drop on map");
        foreach (var t in mapDisplay)
        {
            // 在地图上的物品点击后标记为“装载”
            bool isUnloading = toUnload.Contains(t);
            DrawRow(new Rect(0f, curY, viewRect.width, 32f), t, "\u25B2", isUnloading ? Color.yellow : Color.white, () => {
                if (isUnloading) toUnload.Remove(t); // 如果是计划卸载的，撤销卸载
                else if (projectedCount < maxCapacity) toLoad.Add(t); // 否则标记装载（检查容量）
                else Messages.Message("Reach max capacity", MessageTypeDefOf.RejectInput, false);
            });
            curY += 35f;
        }
        Widgets.EndScrollView();

        // --- 底部确认 ---
        Rect okRect = new Rect(inRect.width / 4f, inRect.height - 50f, inRect.width / 2f, 40f);
        if (Widgets.ButtonText(okRect, "Confirmed"))
        {
            onConfirmed?.Invoke(toLoad.ToList(), toUnload.ToList());
            Close();
        }
    }

    private void DrawSectionHeader(ref float y, float width, string label)
    {
        Rect rect = new Rect(0f, y, width, 28f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, label);
        Text.Anchor = TextAnchor.UpperLeft;
        y += 30f;
    }

    private void DrawRow(Rect rect, Thing thing, string icon, Color textColor, Action onClick)
    {
        Widgets.DrawHighlightIfMouseover(rect);
        GUI.color = textColor;
        
        // 物品图标
        Widgets.ThingIcon(new Rect(rect.x + 5f, rect.y, 32f, 32f), thing);
        // 物品标签
        Widgets.Label(new Rect(rect.x + 45f, rect.y, rect.width - 85f, 32f), thing.LabelCap);
        // 按钮
        if (Widgets.ButtonText(new Rect(rect.xMax - 35f, rect.y, 32f, 32f), icon, true, true, true))
        {
            onClick();
        }
        
        if (Widgets.ButtonInvisible(rect))
        {
            onClick();
        }
        
        GUI.color = Color.white;
    }
}