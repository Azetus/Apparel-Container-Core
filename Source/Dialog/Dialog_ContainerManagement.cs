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
        Listing_Standard listing = new Listing_Standard();
        listing.Begin(inRect);

        // 标题
        Text.Font = GameFont.Medium;
        listing.Label(ownerComp.parent.LabelCap);
        Text.Font = GameFont.Small;

        // 容量显示 
        listing.Gap(5f);
        Rect countRect = listing.GetRect(25f);
        if (ownerComp.ContainerCount >= ownerComp.Props.storageCapacity) GUI.color = ColorLibrary.RedReadable;
        Widgets.Label(countRect, $"{"ACC_Capacity_label".Translate()}: {ownerComp.ContainerCount} / {ownerComp.Props.storageCapacity}");
        GUI.color = Color.white;

        // --- 分段进度条 ---
        Rect barRect = listing.GetRect(12f);
        DrawSegmentedProgressBar(barRect, ownerComp.ContainerCount, ownerComp.Props.storageCapacity);
        listing.Gap(4f); 
        
        // --- DropAll Btn ---
        Rect actionRowRect = listing.GetRect(26f); 
        float btnWidth = 100f; 
        Rect dropAllRect = new Rect(actionRowRect.xMax - btnWidth, actionRowRect.y, btnWidth, actionRowRect.height);
        if (Widgets.ButtonText(dropAllRect, "ACC_DropAll_label".Translate()))
        {
            DropAllItems();
        }
        TooltipHandler.TipRegion(dropAllRect, "ACC_DropAll_Desc".Translate());
        listing.GapLine(15f); 
        listing.Gap(4f);
        // --- 滚动列表区 ---
        Rect scrollRect = listing.GetRect(inRect.height - listing.CurHeight - 50f);
        DrawScrollArea(scrollRect);
        listing.GapLine(15f); 
        if (listing.ButtonText("ACC_Btn_Close_label".Translate())) 
        {
            this.Close();
        }
        
        listing.End();
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
    
    private void DrawScrollArea(Rect outRect)
    {
        IReadOnlyList<T> items = ownerComp.ContainedThings;
        
        float rowHeight = 40f;
        float viewHeight = items.Count * rowHeight;
        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        float currentY = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            T thing = items[i];
            Rect rowRect = new Rect(0f, currentY, viewRect.width, rowHeight - 4f); 
            if (currentY + rowHeight >= scrollPosition.y && currentY <= scrollPosition.y + outRect.height)
            {
                DrawItemRow(rowRect, thing);
            }
            currentY += rowHeight;
        }

        Widgets.EndScrollView();
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

    private void DropAllItems()
    {
        Map map = ownerComp.parent.MapHeld;
        if(map != null)
            ownerComp.TryDropAll(map);
    }
    
}