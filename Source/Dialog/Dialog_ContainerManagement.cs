using ACC_ApparelContainerCore.Comps;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ACC_ApparelContainerCore.Dialog;

public class Dialog_ContainerManagement<T, TP> : Window
    where T : Thing
    where TP : CompProperties_ThingHolderContainer
{
    private readonly Comp_ThingHolderContainer<T, TP> ownerComp;
    private Vector2 scrollPosition = Vector2.zero;

    public override Vector2 InitialSize => new Vector2(500f, 650f);

    public Dialog_ContainerManagement(Comp_ThingHolderContainer<T, TP> comp)
    {
        this.ownerComp = comp;

        // 基础窗口属性
        this.doCloseX = true;
        this.closeOnClickedOutside = true;
        this.absorbInputAroundWindow = false;
        this.forcePause = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        // --- 头部区域 ---
        Text.Font = GameFont.Medium;
        Rect titleRect = new Rect(0f, 0f, inRect.width, 35f);
        // 显示父物品名称
        Widgets.Label(titleRect, ownerComp.parent.LabelCap);

        Text.Font = GameFont.Small;
        int currentCount = ownerComp.ContainerCount;
        int maxCapacity = ownerComp.Props.storageCapacity;

        Rect countRect = new Rect(0f, 40f, inRect.width, 25f);
        if (currentCount >= maxCapacity) GUI.color = ColorLibrary.RedReadable;
        Widgets.Label(countRect, $"{"ACC_Capacity_label".Translate()}: {currentCount} / {maxCapacity}");
        GUI.color = Color.white;

        // --- 分段进度条 ---
        Rect barRect = new Rect(0f, 70f, inRect.width, 12f);
        DrawSegmentedProgressBar(barRect, currentCount, maxCapacity);

        Widgets.DrawLineHorizontal(0f, 95f, inRect.width);

        // --- 滚动列表区 ---
        Rect outRect = new Rect(0f, 105f, inRect.width, inRect.height - 160f);

        // 包内的物品
        IReadOnlyList<T> items = ownerComp.ContainedThings;
        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, items.Count * 40f);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        float num = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            T thing = items[i];
            DrawItemRow(new Rect(0f, num, viewRect.width, 36f), thing);
            num += 40f;
        }

        Widgets.EndScrollView();

        // --- 底部按钮 ---
        Rect closeRect = new Rect(inRect.width / 2f - 60f, inRect.height - 45f, 120f, 35f);
        if (Widgets.ButtonText(closeRect, "ACC_Btn_Close_label".Translate()))
        {
            this.Close();
        }
    }

    private void DrawSegmentedProgressBar(Rect rect, int current, int max)
    {
        Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
        float gap = 2f;
        float segmentWidth = (rect.width - (Math.Max(0, max - 1)) * gap) / max;

        if (segmentWidth < 4f)
        {
            Widgets.FillableBar(rect, (float)current / max);
            return;
        }

        for (int i = 0; i < max; i++)
        {
            Rect segRect = new Rect(rect.x + i * (segmentWidth + gap), rect.y, segmentWidth, rect.height);
            Widgets.DrawBoxSolid(segRect, (i < current) ? ColorLibrary.Aqua : new Color(0.3f, 0.3f, 0.3f));
        }
    }

    private void DrawItemRow(Rect rect, T thing)
    {
        Widgets.DrawHighlightIfMouseover(rect);
        Widgets.ThingIcon(new Rect(rect.x + 5f, rect.y + 2f, 32f, 32f), thing);

        Text.Anchor = TextAnchor.MiddleLeft;
        Rect labelRect = new Rect(rect.x + 45f, rect.y, rect.width - 150f, rect.height);
        Widgets.Label(labelRect, thing.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;

        Rect dropRect = new Rect(rect.xMax - 85f, rect.y + 3f, 80f, rect.height - 6f);
        if (Widgets.ButtonText(dropRect, "ACC_Btn_Drop_label".Translate()))
        {
            DropAction(thing);
        }
    }

    private void DropAction(T thing)
    {
        Map map = ownerComp.parent.MapHeld;
        IntVec3 pos = ownerComp.parent.PositionHeld;
        if (map == null) return;
        if (ownerComp.TryDrop(thing, pos, map, ThingPlaceMode.Near, out _))
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            Messages.Message("ACC_Message_Dropped".Translate(thing.LabelShort), MessageTypeDefOf.CautionInput, false);
        }
    }
}