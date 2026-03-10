using ACC_ApparelContainerCore.ACC_Utility;
using ACC_ApparelContainerCore.Comps.Props;
using RimWorld;
using UnityEngine;
using Verse;

namespace ACC_ApparelContainerCore.Settings.SettingUI;

[StaticConstructorOnStartup]
public static class ACC_SettingsWindowContents
{
    // 缓存全游戏的 Apparel Defs
    private static List<ThingDef> allApparelCached;

    // 主 UI 滚动条
    private static Vector2 mainScrollPos = Vector2.zero;
    private const float topSectionHeight = 40f;

    // 滚动条位置
    private static Vector2 scrollPosLeft = Vector2.zero;
    private static Vector2 scrollPosWhite = Vector2.zero;
    private static Vector2 scrollPosBlack = Vector2.zero;

    // 搜索框内容
    private static string filterLeft = "";
    private static string filterWhite = "";
    private static string filterBlack = "";

    // 选中的项目（用于批量操作）
    private static HashSet<string> selectedLeft = new HashSet<string>();
    private static HashSet<string> selectedWhite = new HashSet<string>();
    private static HashSet<string> selectedBlack = new HashSet<string>();


    static ACC_SettingsWindowContents()
    {
        RefreshApparelCache();
    }

    public static void RefreshApparelCache()
    {
        allApparelCached = DefDatabase<ThingDef>.AllDefs
            .Where(d => d.IsApparel && !d.comps.Any(c => c is CompProperties_ThingHolderContainer))
            .OrderBy(d => d.label)
            .ToList();
    }

    public static void ClearAllSelected()
    {
        selectedLeft.Clear();
        selectedWhite.Clear();
        selectedBlack.Clear();
    }

    public const float margin = 10f;
    public const float leftRatio = 0.45f;
    public const float rightRatio = 0.45f;
    public const float rightStart = 1 - rightRatio;

    public const float midTransferRectHeight = 50f;
    public const float paddingOffset = 5f;


    public static void DoSettingsWindowContents(Rect inRect, ref ApparelContainerCoreSetting settings)
    {
        // --- 1. 滚动容器定义 ---
        // outRect 可用区域
        Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
        // viewRect 可视区域
        Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, inRect.height + topSectionHeight);

        Widgets.BeginScrollView(outRect, ref mainScrollPos, viewRect);

        // 顶部控制栏 
        Rect topBarRect = new Rect(0f, 0f, viewRect.width, topSectionHeight);
        DrawTopControlBar(topBarRect, settings);

        // 绘制分割线
        float lineY = topBarRect.yMax + 5f;
        Widgets.DrawLineHorizontal(0f, lineY, viewRect.width);

        // mainArea 
        float mainAreaY = lineY + 10f;
        Rect mainArea = new Rect(0f, mainAreaY, viewRect.width, viewRect.height - mainAreaY);
        DrawMainThreeColumnArea(mainArea, ref settings);

        Widgets.EndScrollView();
    }

    private static void DrawTopControlBar(Rect rect, ApparelContainerCoreSetting settings)
    {
        float curX = rect.x;

        // 严格模式
        Widgets.CheckboxLabeled(new Rect(curX, rect.y, 140f, 30f), "ACC_Setting_StrictMode_label".Translate(), ref settings.useStrictWhitelist);
        TooltipHandler.TipRegion(new Rect(curX, rect.y, 140f, 30f), "ACC_Setting_StrictMode_desc".Translate());
        curX += 150f;
        // 刷新按钮
        if (Widgets.ButtonText(new Rect(curX, rect.y, 110f, 30f), "ACC_Setting_Refresh_label".Translate()))
        {
            RefreshApparelCache();
            Messages.Message("ACC_Setting_Message_Refresh".Translate(allApparelCached.Count), MessageTypeDefOf.TaskCompletion, false);
        }

        TooltipHandler.TipRegion(new Rect(curX, rect.y, 110f, 30f), "ACC_Setting_Refresh_desc".Translate());

        curX += 150f;
        // 清理按钮
        if (Widgets.ButtonText(new Rect(curX, rect.y, 110f, 30f), "ACC_Setting_ClearList_label".Translate()))
        {
            settings.DoCleanup();
        }

        TooltipHandler.TipRegion(new Rect(curX, rect.y, 110f, 30f), "ACC_Setting_ClearList_desc".Translate());
    }


    public static void DrawMainThreeColumnArea(Rect inRect, ref ApparelContainerCoreSetting settings)
    {
        // 基础间距与比例
        Rect leftRect = new Rect(inRect.x, inRect.y, inRect.width * leftRatio - margin, inRect.height);
        Rect rightRect = new Rect(inRect.width * rightStart + margin, inRect.y, inRect.width * rightRatio - margin, inRect.height);
        Rect middleRect = new Rect(leftRect.xMax, inRect.y, rightRect.x - leftRect.xMax, inRect.height);

        // --- 左侧：源列表 ---
        DrawCustomList(leftRect, "ACC_Setting_ItemSource_label".Translate(), ref filterLeft, ref scrollPosLeft,
            allApparelCached, selectedLeft, () =>
            {
                selectedWhite.Clear();
                selectedBlack.Clear();
            });

        // --- 中间：左右转移按钮 ---
        DrawMainTransferButtons(middleRect, settings);

        // --- 右侧：白名单与黑名单 ---
        float subListHeight = (rightRect.height - (midTransferRectHeight + paddingOffset * 2)) / 2f; // 减去中间按钮高度

        // 白名单区
        Rect whiteRect = new Rect(rightRect.x, rightRect.y, rightRect.width, subListHeight);
        var whiteDefs = settings.whitelist
            .Select(name => DefDatabase<ThingDef>.GetNamedSilentFail(name))
            .Where(d => d != null);
        DrawCustomList(whiteRect, "ACC_Setting_Whitelist_label".Translate(), ref filterWhite, ref scrollPosWhite,
            whiteDefs, selectedWhite, () =>
            {
                selectedLeft.Clear();
                selectedBlack.Clear();
            });

        // 黑白互转按钮区
        Rect midTransferRect = new Rect(rightRect.x, whiteRect.yMax + paddingOffset, rightRect.width, midTransferRectHeight);
        DrawInternalTransferButtons(midTransferRect, settings);

        // 黑名单区
        Rect blackRect = new Rect(rightRect.x, midTransferRect.yMax + paddingOffset, rightRect.width, subListHeight);
        var blackDefs = settings.blacklist.Select(name => DefDatabase<ThingDef>.GetNamedSilentFail(name))
            .Where(d => d != null);
        DrawCustomList(blackRect, "ACC_Setting_Blacklist_label".Translate(), ref filterBlack, ref scrollPosBlack,
            blackDefs, selectedBlack, () =>
            {
                selectedLeft.Clear();
                selectedWhite.Clear();
            });
    }

    public const float listHeaderHeight = 24f;
    public const float listItemHeight = 26f;
    public const float listItemPadding = 1f;
    public const float listItemTotalHeight = listItemHeight + listItemPadding * 2;
    public const float iconSideLength = 22f;

    // 通用Block，搜索栏 + 滚动列表
    private static void DrawCustomList(Rect rect, string header, ref string filter, ref Vector2 scrollPos,
        IEnumerable<ThingDef> items, HashSet<string> selectedSet, Action onFocus)
    {
        var itemList = items.ToList();
        // 标题
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, listHeaderHeight), header);

        // 选择所有
        float ClearButtonWidth = rect.width / 5f;
        if (Widgets.ButtonText(new Rect(rect.x + (ClearButtonWidth * 4f), rect.y, ClearButtonWidth, listHeaderHeight),
                "ACC_Setting_SelectAll_label".Translate()))
        {
            onFocus?.Invoke();
            foreach (var def in itemList)
                selectedSet.Add(def.defName);
        }

        // 搜索框
        filter = Widgets.TextField(new Rect(rect.x, rect.y + listHeaderHeight + 1f, rect.width, listHeaderHeight), filter);

        // 过滤逻辑
        string currentFilter = filter;
        List<ThingDef> filteredItems = itemList
            .Where(d => currentFilter.NullOrEmpty() ||
                        d.label.ToLower().Contains(currentFilter.ToLower())
            )
            .ToList();

        // 滚动视图区域
        Rect outRect = new Rect(rect.x, rect.y + 55f, rect.width, rect.height - 55f);
        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, filteredItems.Count * listItemTotalHeight);

        Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
        float num = 0f;
        foreach (var def in filteredItems)
        {
            Rect rowRect = new Rect(0f, num, viewRect.width, listItemHeight);

            // 背景与选中状态
            if (selectedSet.Contains(def.defName))
                Widgets.DrawHighlightSelected(rowRect);
            else
                Widgets.DrawHighlightIfMouseover(rowRect);

            // 点击切换
            if (Widgets.ButtonInvisible(rowRect))
            {
                // 执行失焦逻辑：清空其他列表的选中状态
                onFocus?.Invoke();
                if (!selectedSet.Add(def.defName))
                    selectedSet.Remove(def.defName);
            }

            // 图标 + 标签
            Widgets.ThingIcon(new Rect(5f, num + (listItemHeight - iconSideLength) / 2f, iconSideLength, iconSideLength), def);
            Rect labelRect = new Rect(32f, num, viewRect.width - 32f, listItemHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, def.label.CapitalizeFirst());
            Text.Anchor = TextAnchor.UpperLeft;

            num += listItemTotalHeight;
        }

        Widgets.EndScrollView();
    }


    public const float btnHeight = 30f;

    // 中间按钮区
    private static void DrawMainTransferButtons(Rect rect, ApparelContainerCoreSetting settings)
    {
        float autoAddY = rect.y + (rect.height / 3f) - btnHeight * 4f;
        if (Widgets.ButtonText(new Rect(rect.x + 5f, autoAddY, rect.width - 10f, btnHeight), "ACC_Setting_AutoAdd_Btn_label".Translate()))
        {
            DoAutoAddLogic(settings);
            selectedLeft.Clear();
        }

        float firstY = rect.y + (rect.height / 3f) - (btnHeight / 2f);
        // [左 -> 白]
        if (Widgets.ButtonText(new Rect(rect.x + 5f, firstY, rect.width - 10f, btnHeight), "->>"))
        {
            foreach (var name in selectedLeft)
            {
                // 互斥处理
                settings.whitelist.Add(name);
                settings.blacklist.Remove(name);
            }

            selectedLeft.Clear();
        }

        float middleY = rect.y + (rect.height / 2f) - (btnHeight / 2f);
        // [从名单移除] (右 -> 左)
        if (Widgets.ButtonText(new Rect(rect.x + 5f, middleY, rect.width - 10f, btnHeight), "<<-"))
        {
            foreach (var name in selectedWhite) settings.whitelist.Remove(name);
            foreach (var name in selectedBlack) settings.blacklist.Remove(name);
            selectedWhite.Clear();
            selectedBlack.Clear();
        }

        float lowerY = rect.y + (rect.height * 2f / 3f) - (btnHeight / 2f);
        // [左 -> 黑]
        if (Widgets.ButtonText(new Rect(rect.x + 5f, lowerY, rect.width - 10f, btnHeight), "->>"))
        {
            foreach (var name in selectedLeft)
            {
                settings.blacklist.Add(name);
                settings.whitelist.Remove(name);
            }

            selectedLeft.Clear();
        }
    }

    private static void DrawInternalTransferButtons(Rect rect, ApparelContainerCoreSetting settings)
    {
        // float w = rect.width / 2f - 5f;
        float middle = rect.width / 2f;
        // [白 -> 黑]
        // if (Widgets.ButtonText(new Rect(rect.x, rect.y + 10f, w, btnHeight), "↓ 移至黑名单"))
        if (Widgets.ButtonImage(new Rect(rect.x + middle / 2f, rect.y + 10f, btnHeight, btnHeight), TexButton.ReorderDown))
        {
            foreach (var name in selectedWhite)
            {
                settings.whitelist.Remove(name);
                settings.blacklist.Add(name);
            }

            selectedWhite.Clear();
        }

        // [黑 -> 白]
        // if (Widgets.ButtonText(new Rect(rect.x + w + 10f, rect.y + 10f, w, btnHeight), "↑ 移至白名单"))
        if (Widgets.ButtonImage(new Rect(rect.x + (middle * 1.5f), rect.y + 10f, btnHeight, btnHeight), TexButton.ReorderUp))
        {
            foreach (var name in selectedBlack)
            {
                settings.blacklist.Remove(name);
                settings.whitelist.Add(name);
            }

            selectedBlack.Clear();
        }
    }

    public static void DoAutoAddLogic(ApparelContainerCoreSetting settings)
    {
        // 弹出确认框防止误触
        Find.WindowStack.Add(new Dialog_MessageBox(
            "ACC_Setting_AutoAdd_DialogMessage".Translate(),
            "ACC_Btn_Confirm_label".Translate(), () =>
            {
                int addedCount = 0;
                foreach (var def in allApparelCached)
                {
                    // 排除关键词防护：护盾类通常不应进入白名单
                    if ((def.defName.ToLower().Contains("shield") || def.label.ToLower().Contains("shield")) &&
                        !(def.defName.ToLower().Contains("pack") || def.label.ToLower().Contains("pack"))) continue;
                    // 不许套娃
                    if (def.comps.Any(c => c is CompProperties_ThingHolderContainer)) continue;

                    bool hasVerb = !def.Verbs.NullOrEmpty();
                    bool hasAbility = !def.apparel.abilities.NullOrEmpty();
                    bool hasFunctionalComp = def.comps.Any(c =>
                        c is CompProperties_Usable or CompProperties_ApparelReloadable or CompProperties_Rechargeable);
                    bool modifiedGizmo = def.comps.Any(c => UtilityChecker.IsCompPotentiallyFunctional(c.compClass));

                    if ((hasVerb || hasAbility || hasFunctionalComp || modifiedGizmo) && !settings.whitelist.Contains(def.defName))
                    {
                        settings.whitelist.Add(def.defName);
                        settings.blacklist.Remove(def.defName); // 确保不在黑名单冲突
                        addedCount++;
                    }
                }

                Messages.Message($"ACC_Setting_Message_AutoComplete".Translate(addedCount), MessageTypeDefOf.TaskCompletion);
            }, "ACC_Btn_Cancel_label".Translate()));
    }
}