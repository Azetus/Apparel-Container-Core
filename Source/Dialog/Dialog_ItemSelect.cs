using RimWorld;
using UnityEngine;
using Verse;

namespace ACC_ApparelContainerCore.Dialog;

public struct TransferRequest
{
    public Thing thing;
    public int count;
}

public class Dialog_ItemSelect : Window
{
    private readonly Dictionary<Thing, int> transferCounts = new Dictionary<Thing, int>();
    private readonly List<Thing> allItems; // 合并后的唯一物品列表
    private readonly HashSet<Thing> initiallyInBag;

    private readonly Action<List<TransferRequest>, List<TransferRequest>> onConfirmed;
    private readonly int maxCapacity;
    private string searchTerm = "";
    private Vector2 scrollPosition = Vector2.zero;

    public override Vector2 InitialSize => new Vector2(700f, 700f);

    public Dialog_ItemSelect(IEnumerable<Thing> mapItems, IReadOnlyList<Thing> containerItems, int capacity,
        Action<List<TransferRequest>, List<TransferRequest>> onConfirmed)
    {
        this.initiallyInBag = containerItems.ToHashSet();
        this.maxCapacity = capacity;
        this.onConfirmed = onConfirmed;

        // 初始化计数器：如果在包里，计数 = 当前堆叠数；如果在地上，计数 = 0
        foreach (var t in containerItems) transferCounts[t] = t.stackCount;
        foreach (var t in mapItems) transferCounts.TryAdd(t, 0);

        this.allItems = transferCounts.Keys.ToList();
        this.doCloseX = true;
        this.closeOnClickedOutside = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        // 1. 计算当前分配的总量
        int currentTotal = transferCounts.Values.Sum();

        // --- 头部区域 ---
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Container management");

        Text.Font = GameFont.Small;
        // 如果超过容量，显示红色数字
        GUI.color = currentTotal > maxCapacity ? ColorLibrary.RedReadable : Color.white;
        Widgets.Label(new Rect(0f, 35f, inRect.width, 25f), $"capacity: {currentTotal} / {maxCapacity}");
        GUI.color = Color.white;

        // 固定宽度为 300，高度为 30
        Rect searchRect = new Rect(0f, 65f, 300f, 30f);
        searchTerm = Widgets.TextField(searchRect, searchTerm);

        // --- 列表 ---
        Rect outRect = new Rect(0f, 105f, inRect.width, inRect.height - 180f);
        var filteredList = allItems.Where(t => t.Label.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, filteredList.Count * 35f);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        float num = 0f;
        foreach (var t in filteredList)
        {
            DrawTransferRow(new Rect(0f, num, viewRect.width, 32f), t, ref currentTotal);
            num += 35f;
        }

        Widgets.EndScrollView();

        // --- 底部按钮 ---
        Rect okRect = new Rect(inRect.width / 2f - 100f, inRect.height - 50f, 200f, 40f);
        if (Widgets.ButtonText(okRect, "Confirm"))
        {
            if (currentTotal <= maxCapacity)
            {
                GenerateAndConfirm();
            }
            else
            {
                Messages.Message("reach max capacity", MessageTypeDefOf.RejectInput, false);
            }
        }
    }

    private void DrawTransferRow(Rect rect, Thing t, ref int total)
    {
        Widgets.DrawHighlightIfMouseover(rect);

        // 物品图标与名称
        Widgets.ThingIcon(new Rect(rect.x, rect.y, 32f, 32f), t);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(rect.x + 40f, rect.y, 250f, 32f), t.LabelCap);

        // 来源标识
        GUI.color = initiallyInBag.Contains(t) ? Color.cyan : Color.gray;
        Widgets.Label(new Rect(rect.x + 300f, rect.y, 120f, 32f),
            initiallyInBag.Contains(t) ? "[In Container]" : "[On Map]");
        GUI.color = Color.white;

        // --- 控制器区域 ---
        float controlStartX = rect.xMax - 180f;
        int curCount = transferCounts[t];

        // 左按钮：增加包内数量
        if (Widgets.ButtonText(new Rect(controlStartX, rect.y, 30f, 32f), "<"))
        {
            if (curCount < t.stackCount && total < maxCapacity)
            {
                transferCounts[t]++;
                total++;
            }
        }

        // 中间显示分配数量
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(controlStartX + 35f, rect.y, 50f, 32f), curCount.ToString());

        // 右按钮：减少包内数量
        if (Widgets.ButtonText(new Rect(controlStartX + 90f, rect.y, 30f, 32f), ">"))
        {
            if (curCount > 0)
            {
                transferCounts[t]--;
                total--;
            }
        }

        // 显示最大可用堆叠
        GUI.color = Color.gray;
        Widgets.Label(new Rect(controlStartX + 125f, rect.y, 50f, 32f), $"/ {t.stackCount}");
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }


    private void GenerateAndConfirm()
    {
        List<TransferRequest> toLoad = new List<TransferRequest>();
        List<TransferRequest> toUnload = new List<TransferRequest>();

        foreach (var kvp in transferCounts)
        {
            Thing t = kvp.Key;
            int targetCount = kvp.Value;
            bool wasInBag = initiallyInBag.Contains(t);

            if (wasInBag)
            {
                // 如果原本在包里，但现在的目标数量比原来少 -> 卸载
                if (targetCount < t.stackCount)
                {
                    toUnload.Add(new TransferRequest { thing = t, count = t.stackCount - targetCount });
                }
            }
            else
            {
                // 如果原本在地上，现在的目标数量大于 0 -> 装载
                if (targetCount > 0)
                {
                    toLoad.Add(new TransferRequest { thing = t, count = targetCount });
                }
            }
        }

        onConfirmed(toLoad, toUnload);
        Close();
    }
}