using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ResearchScene 전용 조립형 화면입니다. 배경과 프리팹 디자인이 준비되면 이 컨트롤러의
/// 성장 데이터와 구매 흐름을 그대로 두고 UI만 교체할 수 있습니다.
/// </summary>
public sealed class ResearchSceneController : MonoBehaviour
{
    [SerializeField] private ResearchData[] researchEntries = Array.Empty<ResearchData>();
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TowerData[] towers = Array.Empty<TowerData>();
    [SerializeField] private SkillData[] skills = Array.Empty<SkillData>();
    [SerializeField] private Sprite workshopBackground;
    [SerializeField] private Sprite skillPointIcon;

    private readonly Dictionary<ResearchData, Button> researchButtons = new Dictionary<ResearchData, Button>();
    private TextMeshProUGUI skillPointText;
    private TextMeshProUGUI detailTitleText;
    private TextMeshProUGUI detailDescriptionText;
    private TextMeshProUGUI detailCategoryText;
    private readonly ResearchStatRowView[] statRows = new ResearchStatRowView[6];
    private ResearchViewFactory view;
    private TextMeshProUGUI detailPurchaseText;
    private Image detailIcon;
    private Button purchaseButton;
    private TextMeshProUGUI purchaseButtonLabel;
    private TextMeshProUGUI noticeText;
    private ResearchData selectedResearch;
    private TowerData selectedTower;
    private SkillData selectedSkill;
    private RectTransform researchOptions;
    private readonly Dictionary<UnityEngine.Object, ResearchTargetView> targetViews =
        new Dictionary<UnityEngine.Object, ResearchTargetView>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private GameObject testTools;
    private bool testToolsShortcutHeld;
#endif

    private static readonly Color BackgroundColor = new Color(0.065f, 0.075f, 0.085f);
    private static readonly Color PanelColor = new Color(0.085f, 0.115f, 0.14f, 0.96f);
    private static readonly Color TowerColor = new Color(0.93f, 0.72f, 0.40f);
    private static readonly Color SkillColor = new Color(0.46f, 0.78f, 0.91f);
    private static readonly Color AccentColor = new Color(0.94f, 0.75f, 0.38f);
    private static readonly Color MutedColor = new Color(0.63f, 0.68f, 0.70f);
    private static readonly Color EdgeColor = new Color(0.38f, 0.34f, 0.27f);
    private static readonly Color CardColor = new Color(0.13f, 0.17f, 0.19f);

    private void Awake()
    {
        BuildScreen();
        if (towers.Length > 0)
            SelectTarget(towers[0], null);
        else if (skills.Length > 0)
            SelectTarget(null, skills[0]);
        Refresh();
    }

    private void BuildScreen()
    {
        view = new ResearchViewFactory(koreanFont);
        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        RectTransform background = CreatePanel("Background", transform, BackgroundColor);
        ResearchViewFactory.Place(background, Vector2.zero, Vector2.one);

        if (workshopBackground != null)
        {
            Image backdrop = CreateIcon("Workshop", background, workshopBackground, Vector2.zero, Vector2.one);
            backdrop.preserveAspect = false;
        }
        RectTransform shade = CreatePanel("Shade", background, new Color(0.015f, 0.025f, 0.035f, 0.38f));
        ResearchViewFactory.Place(shade, Vector2.zero, Vector2.one);
        view.Frame("Header", background, new Color(0.06f, 0.09f, 0.11f, 0.94f), EdgeColor,
            new Vector2(0.045f, 0.87f), new Vector2(0.955f, 0.975f));
        CreateText("SceneTitle", background, "전술 연구소", 48,
            TextAlignmentOptions.Left, new Color(0.96f, 0.9f, 0.77f), new Vector2(0.065f, 0.914f), new Vector2(0.43f, 0.965f));
        CreateText("SceneGuide", background, "전투를 준비하는 영구 연구", 23,
            TextAlignmentOptions.Left, MutedColor, new Vector2(0.066f, 0.88f), new Vector2(0.47f, 0.918f));
        RectTransform points = view.Frame("PointWallet", background, new Color(0.035f, 0.075f, 0.095f),
            new Color(0.32f, 0.55f, 0.64f), new Vector2(0.745f, 0.887f), new Vector2(0.935f, 0.958f));
        CreateIcon("SkillPointIcon", points, skillPointIcon,
            new Vector2(0.025f, 0.12f), new Vector2(0.185f, 0.88f));
        skillPointText = CreateText("SkillPoints", points, string.Empty, 30,
            TextAlignmentOptions.Right, new Color(0.86f, 0.95f, 1f), new Vector2(0.20f, 0.05f), new Vector2(0.93f, 0.95f));

        RectTransform towerPanel = view.Frame("TowerResearch", background, PanelColor, EdgeColor,
            new Vector2(0.045f, 0.145f), new Vector2(0.28f, 0.835f));
        BuildResearchPanel(towerPanel, ResearchTargetType.Tower, "타워 연구", TowerColor);
        RectTransform detailPanel = view.Frame("ResearchDetail", background,
            new Color(0.075f, 0.105f, 0.13f, 0.98f), new Color(0.6f, 0.48f, 0.29f),
            new Vector2(0.30f, 0.145f), new Vector2(0.70f, 0.835f));
        BuildDetailPanel(detailPanel);
        RectTransform skillPanel = view.Frame("SkillResearch", background, PanelColor, EdgeColor,
            new Vector2(0.72f, 0.145f), new Vector2(0.955f, 0.835f));
        BuildResearchPanel(skillPanel, ResearchTargetType.Skill, "스킬 연구", SkillColor);
        noticeText = CreateText("Notice", background, "", 26,
            TextAlignmentOptions.Center, Color.white, new Vector2(0.29f, 0.075f), new Vector2(0.71f, 0.125f));
        CreateText("PermanentResearchGuide", background, "연구 효과는 다음 전투에도 유지됩니다", 23,
            TextAlignmentOptions.Left, new Color(0.86f, 0.81f, 0.69f), new Vector2(0.045f, 0.032f), new Vector2(0.58f, 0.078f));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        BuildTestTools(background);
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        bool shortcutHeld = Input.GetKey(KeyCode.Q) && Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.E);
        // 세 키를 누른 순간에만 전환하고, 계속 누르고 있는 동안에는 유지합니다.
        if (shortcutHeld && !testToolsShortcutHeld && testTools != null)
            testTools.SetActive(!testTools.activeSelf);
        testToolsShortcutHeld = shortcutHeld;
    }

    private void BuildTestTools(Transform background)
    {
        RectTransform tools = view.Frame("TestTools", background, PanelColor, EdgeColor,
            new Vector2(0.72f, 0.032f), new Vector2(0.955f, 0.078f));
        Button add = view.Button("AddPoint", tools, "+1 SP", CardColor, EdgeColor,
            new Vector2(0.04f, 0.1f), new Vector2(0.47f, 0.9f), 23);
        add.onClick.AddListener(() =>
        {
            ResearchProgressStore.AddTestSkillPoints(1);
            SetNotice("테스트 스킬 포인트를 1 지급했습니다.");
            Refresh();
        });
        Button reset = view.Button("Reset", tools, "테스트 초기화", CardColor, EdgeColor,
            new Vector2(0.53f, 0.1f), new Vector2(0.96f, 0.9f), 22);
        reset.onClick.AddListener(() =>
        {
            ResearchProgressStore.ResetForTesting(researchEntries);
            SetNotice("스킬 포인트와 연구 기록을 초기화했습니다.");
            Refresh();
        });
        testTools = tools.gameObject;
        testTools.SetActive(false);
    }
#endif

    private void BuildResearchPanel(RectTransform panel, ResearchTargetType targetType, string title, Color accent)
    {
        CreateText("Title", panel, title, 38, TextAlignmentOptions.Left,
            accent, new Vector2(0.08f, 0.865f), new Vector2(0.92f, 0.965f));
        CreateText("Guide", panel, "강화할 대상을 선택하세요", 24,
            TextAlignmentOptions.Left, MutedColor, new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.875f));
        view.Rule("HeaderRule", panel, EdgeColor, new Vector2(0.08f, 0.785f), new Vector2(0.92f, 0.788f));
        CreateText("Footer", panel, targetType == ResearchTargetType.Tower ? "모든 단계와 분기에 적용" : "피해량과 효과를 더 강하게", 23,
            TextAlignmentOptions.Center, MutedColor, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.1f));

        List<UnityEngine.Object> filtered = new List<UnityEngine.Object>();
        if (targetType == ResearchTargetType.Tower)
        {
            foreach (TowerData tower in towers)
                if (tower != null) filtered.Add(tower);
        }
        else
        {
            foreach (SkillData skill in skills)
                if (skill != null) filtered.Add(skill);
        }
        if (filtered.Count == 0)
        {
            CreateText("Empty", panel, "등록된 연구가 없습니다.", 22,
                TextAlignmentOptions.Center, Color.white, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.58f));
            return;
        }

        float top = 0.75f;
        float height = Mathf.Min(0.185f, (0.61f - 0.035f * (filtered.Count - 1)) / filtered.Count);
        for (int index = 0; index < filtered.Count; index++)
        {
            UnityEngine.Object target = filtered[index];
            TowerData tower = target as TowerData;
            SkillData skill = target as SkillData;
            float yMax = top - index * (height + 0.035f);
            float yMin = yMax - height;
            Button button = CreateButton("Target_" + target.name, panel, string.Empty,
                CardColor, new Vector2(0.07f, yMin), new Vector2(0.93f, yMax));
            button.onClick.AddListener(() => SelectTarget(tower, skill));
            view.Frame("IconWell", button.transform, new Color(0.055f, 0.085f, 0.11f), EdgeColor,
                new Vector2(0.04f, 0.15f), new Vector2(0.32f, 0.85f));
            Image icon = CreateIcon("Icon", button.transform, tower != null ? tower.icon : skill.Icon,
                new Vector2(0.055f, 0.2f), new Vector2(0.305f, 0.8f));
            if (icon.sprite == null)
                icon.color = accent;

            CreateText("Name", button.transform, tower != null ? tower.towerName.Replace("기본 ", "") : skill.DisplayName,
                32, TextAlignmentOptions.Left, new Color(0.94f, 0.92f, 0.85f),
                new Vector2(0.38f, 0.59f), new Vector2(0.95f, 0.93f));
            TextMeshProUGUI status = CreateText("Status", button.transform, string.Empty, 24,
                TextAlignmentOptions.Left, accent, new Vector2(0.38f, 0.31f), new Vector2(0.95f, 0.6f));
            Image[] pips = new Image[5];
            for (int pip = 0; pip < pips.Length; pip++)
            {
                RectTransform mark = view.Panel("Rank_" + pip, button.transform, MutedColor);
                ResearchViewFactory.Place(mark, new Vector2(0.38f + pip * 0.105f, 0.16f),
                    new Vector2(0.46f + pip * 0.105f, 0.22f));
                pips[pip] = mark.GetComponent<Image>();
            }
            targetViews[target] = new ResearchTargetView { Button = button, Status = status, RankPips = pips, Accent = accent };
        }
    }

    private void BuildDetailPanel(RectTransform panel)
    {
        view.Frame("IconWell", panel, new Color(0.045f, 0.075f, 0.10f), EdgeColor,
            new Vector2(0.045f, 0.78f), new Vector2(0.235f, 0.96f));
        detailIcon = CreateIcon("TargetIcon", panel, null,
            new Vector2(0.06f, 0.795f), new Vector2(0.22f, 0.945f));
        detailCategoryText = CreateText("Category", panel, string.Empty, 22,
            TextAlignmentOptions.Left, AccentColor, new Vector2(0.28f, 0.925f), new Vector2(0.94f, 0.97f));
        detailTitleText = CreateText("Title", panel, string.Empty, 38,
            TextAlignmentOptions.Left, new Color(0.98f, 0.94f, 0.84f), new Vector2(0.28f, 0.85f), new Vector2(0.95f, 0.925f));
        detailDescriptionText = CreateText("Description", panel, string.Empty, 25,
            TextAlignmentOptions.Left, MutedColor, new Vector2(0.28f, 0.76f), new Vector2(0.95f, 0.845f));
        view.Rule("HeaderRule", panel, EdgeColor, new Vector2(0.06f, 0.725f), new Vector2(0.94f, 0.728f));
        CreateText("StatsHeading", panel, "능력치", 24, TextAlignmentOptions.Left,
            MutedColor, new Vector2(0.075f, 0.674f), new Vector2(0.46f, 0.719f));
        CreateText("BaseHeading", panel, "기본", 24, TextAlignmentOptions.Right,
            MutedColor, new Vector2(0.46f, 0.674f), new Vector2(0.67f, 0.719f));
        CreateText("CurrentHeading", panel, "현재", 24, TextAlignmentOptions.Right,
            AccentColor, new Vector2(0.70f, 0.674f), new Vector2(0.925f, 0.719f));
        RectTransform stats = view.Panel("Stats", panel, Color.clear);
        ResearchViewFactory.Place(stats, new Vector2(0.075f, 0.42f), new Vector2(0.925f, 0.665f));
        for (int index = 0; index < statRows.Length; index++)
        {
            RectTransform row = view.Panel("Stat_" + index, stats, Color.clear);
            float top = 1f - index / 6f;
            ResearchViewFactory.Place(row, new Vector2(0f, top - 1f / 6f), new Vector2(1f, top));
            statRows[index] = new ResearchStatRowView
            {
                Root = row,
                Name = view.Label("Name", row, "", 26, TextAlignmentOptions.Left, Color.white, Vector2.zero, new Vector2(0.44f, 1f)),
                Base = view.Label("Base", row, "", 26, TextAlignmentOptions.Right, MutedColor, new Vector2(0.44f, 0f), new Vector2(0.7f, 1f)),
                Current = view.Label("Current", row, "", 26, TextAlignmentOptions.Right, Color.white, new Vector2(0.72f, 0f), Vector2.one)
            };
        }
        view.Rule("ResearchRule", panel, EdgeColor, new Vector2(0.06f, 0.402f), new Vector2(0.94f, 0.405f));
        CreateText("ResearchHeading", panel, "강화할 능력치", 26,
            TextAlignmentOptions.Left, AccentColor, new Vector2(0.075f, 0.35f), new Vector2(0.93f, 0.399f));
        researchOptions = CreatePanel("ResearchOptions", panel, Color.clear);
        ResearchViewFactory.Place(researchOptions, new Vector2(0.075f, 0.28f), new Vector2(0.935f, 0.35f));
        detailPurchaseText = CreateText("PurchaseInfo", panel, string.Empty, 28,
            TextAlignmentOptions.Center, Color.white, new Vector2(0.06f, 0.135f), new Vector2(0.94f, 0.27f));
        purchaseButton = view.Button("Purchase", panel, "연구하기", AccentColor, new Color(1f, 0.87f, 0.58f),
            new Vector2(0.075f, 0.037f), new Vector2(0.925f, 0.124f), 31, new Color(0.16f, 0.12f, 0.075f));
        purchaseButtonLabel = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
        purchaseButton.onClick.AddListener(TryPurchaseSelected);
        BuildResearchOptions();
    }

    private void TryPurchaseSelected()
    {
        if (selectedResearch == null)
            return;

        if (ResearchProgressStore.TryPurchase(selectedResearch, out string failureMessage))
            SetNotice($"{selectedResearch.DisplayName} 연구를 완료했습니다.");
        else
            SetNotice(failureMessage);

        Refresh();
    }

    private void SelectResearch(ResearchData research)
    {
        selectedResearch = research;
        SetNotice(string.Empty);
        Refresh();
    }

    private static bool MatchesTarget(ResearchData research, TowerData tower, SkillData skill)
    {
        if (research == null) return false;
        return tower != null ? research.TargetType == ResearchTargetType.Tower && research.TargetId == tower.id
            : skill != null && research.TargetType == ResearchTargetType.Skill && research.TargetId == skill.SkillId;
    }

    private void SelectTarget(TowerData tower, SkillData skill)
    {
        selectedTower = tower;
        selectedSkill = skill;
        selectedResearch = null;
        SetNotice(string.Empty);
        int optionCount = 0;
        foreach (ResearchData research in researchEntries)
            if (MatchesTarget(research, tower, skill) && researchButtons.ContainsKey(research)) optionCount++;

        int index = 0;
        foreach (var entry in researchButtons)
        {
            bool matches = MatchesTarget(entry.Key, tower, skill);
            entry.Value.gameObject.SetActive(matches);
            if (!matches) continue;

            if (selectedResearch == null) selectedResearch = entry.Key;
            float width = 1f / optionCount;
            ResearchViewFactory.Place(entry.Value.GetComponent<RectTransform>(),
                new Vector2(index * width, 0.08f), new Vector2((index + 1) * width - 0.01f, 0.92f));
            index++;
        }
        Refresh();
    }

    // 선택할 때 TMP 오브젝트를 제거·재생성하지 않고 기존 버튼을 재사용합니다.
    private void BuildResearchOptions()
    {
        foreach (ResearchData research in researchEntries)
        {
            if (research == null || researchButtons.ContainsKey(research)) continue;
            Button button = CreateButton(research.ResearchId, researchOptions,
                research.DisplayName, CardColor, Vector2.zero, Vector2.one);
            button.onClick.AddListener(() => SelectResearch(research));
            researchButtons.Add(research, button);
            button.gameObject.SetActive(false);
        }
    }

    private void Refresh()
    {
        if (skillPointText == null)
            return;

        skillPointText.text = $"스킬 포인트  {ResearchProgressStore.SkillPoints}";
        RefreshTargetCards();
        RefreshResearchButtons();
        RefreshDetail();
    }

    private void RefreshResearchButtons()
    {
        foreach (var entry in researchButtons)
        {
            Image image = entry.Value.GetComponent<Image>();
            if (image != null)
            {
                bool selected = entry.Key == selectedResearch;
                image.color = selected ? new Color(0.20f, 0.24f, 0.24f) : CardColor;
                entry.Value.GetComponent<Outline>().effectColor = selected ? AccentColor : EdgeColor;
            }
        }
    }

    private void RefreshDetail()
    {
        detailCategoryText.text = selectedTower != null ? "타워 연구" : "스킬 연구";
        detailCategoryText.color = selectedTower != null ? TowerColor : SkillColor;
        detailTitleText.text = selectedTower != null ? selectedTower.towerName
            : selectedSkill != null ? selectedSkill.DisplayName : "대상을 선택하세요";
        detailDescriptionText.text = selectedTower != null ? selectedTower.description
            : selectedSkill != null ? selectedSkill.Description : string.Empty;
        detailIcon.sprite = selectedTower != null ? selectedTower.icon
            : selectedSkill != null ? selectedSkill.Icon : null;
        detailIcon.enabled = detailIcon.sprite != null;
        RefreshStatRows();
        if (selectedResearch == null)
        {
            detailPurchaseText.text = "아직 등록된 연구 항목이 없습니다.";
            SetPurchaseAppearance("연구 준비 중", false, false);
            return;
        }

        int level = ResearchProgressStore.GetLevel(selectedResearch.ResearchId);
        bool isMaxLevel = level >= selectedResearch.MaxLevel;

        if (isMaxLevel)
        {
            detailPurchaseText.text = $"<color=#EDC36C>연구 완료   {level} / {selectedResearch.MaxLevel}</color>\n" +
                $"{GetStatLabel(selectedResearch)}  {FormatStat(selectedResearch, ResearchStatResolver.GetResearchStatValue(selectedResearch, level))}";
            SetPurchaseAppearance("최대 강화 완료", false, false);
            return;
        }

        ResearchRank nextRank = selectedResearch.GetRank(level);
        float current = ResearchStatResolver.GetResearchStatValue(selectedResearch, level);
        float next = ResearchStatResolver.GetResearchStatValue(selectedResearch, level + 1);
        detailPurchaseText.text = $"연구 단계   {level} / {selectedResearch.MaxLevel}\n" +
            $"<size=115%>{GetStatLabel(selectedResearch)}  {FormatStat(selectedResearch, current)} → <color=#EDC36C>{FormatStat(selectedResearch, next)}</color></size>\n" +
            $"<size=80%><color=#A5ADB0>{BuildRankEffect(selectedResearch, nextRank)}</color></size>";
        bool affordable = ResearchProgressStore.SkillPoints >= nextRank.skillPointCost;
        SetPurchaseAppearance((affordable ? "연구하기" : "SP 부족") + $"   ·   {nextRank.skillPointCost} SP", true, affordable);
    }

    private void SetPurchaseAppearance(string label, bool interactable, bool highlighted)
    {
        purchaseButtonLabel.text = label;
        purchaseButtonLabel.color = highlighted ? new Color(0.16f, 0.12f, 0.075f) : MutedColor;
        purchaseButton.GetComponent<Image>().color = highlighted ? AccentColor : new Color(0.15f, 0.20f, 0.22f);
        purchaseButton.GetComponent<Outline>().effectColor = highlighted ? new Color(1f, 0.87f, 0.58f) : EdgeColor;
        purchaseButton.interactable = interactable;
    }

    private void RefreshTargetCards()
    {
        foreach (var entry in targetViews)
        {
            ResearchTargetView card = entry.Value;
            bool selected = entry.Key == (UnityEngine.Object)selectedTower || entry.Key == (UnityEngine.Object)selectedSkill;
            card.Button.GetComponent<Image>().color = selected ? new Color(0.20f, 0.24f, 0.24f) : CardColor;
            card.Button.GetComponent<Outline>().effectColor = selected ? card.Accent : EdgeColor;
            int level = 0;
            int maxLevel = 0;
            ResearchData first = null;
            foreach (ResearchData research in researchEntries)
            {
                if (!MatchesTarget(research, entry.Key as TowerData, entry.Key as SkillData)) continue;
                if (first == null) first = research;
                level += Mathf.Clamp(ResearchProgressStore.GetLevel(research.ResearchId), 0, research.MaxLevel);
                maxLevel += research.MaxLevel;
            }
            card.Status.text = first == null ? "연구 준비 중" : $"{GetStatLabel(first)} 연구   {level} / {maxLevel}";
            for (int pip = 0; pip < card.RankPips.Length; pip++)
                card.RankPips[pip].color = maxLevel > 0 && level * card.RankPips.Length >= (pip + 1) * maxLevel
                    ? card.Accent : new Color(0.24f, 0.29f, 0.30f);
        }
    }

    private static string GetStatLabel(ResearchData research)
    {
        switch (research.ModifierType)
        {
            case ResearchModifierType.AttackCooldownPercent: return "공격 간격";
            case ResearchModifierType.AttackRangePercent: return "사거리";
            case ResearchModifierType.PeriodicDamagePercent: return "지속 피해";
            case ResearchModifierType.DurationFlat: return "지속 시간";
            default: return research.TargetType == ResearchTargetType.Tower ? "공격력" : "피해량";
        }
    }

    private static string BuildRankEffect(ResearchData growth, ResearchRank rank)
    {
        string value = rank.modifierValue.ToString("0.##");
        switch (growth.ModifierType)
        {
            case ResearchModifierType.DamageFlat: return growth.TargetType == ResearchTargetType.Tower
                ? $"공격력 +{value}" : $"피해량 +{value}";
            case ResearchModifierType.DamagePercent: return $"피해량 +{value}%";
            case ResearchModifierType.AttackCooldownPercent: return $"공격 간격 -{value}%";
            case ResearchModifierType.AttackRangePercent: return $"사거리 +{value}%";
            case ResearchModifierType.PeriodicDamagePercent: return $"지속 피해 +{value}%";
            case ResearchModifierType.DurationFlat: return $"지속 시간 +{value}초";
            default: return $"수치 +{value}";
        }
    }

    private static string FormatStat(ResearchData research, float value)
    {
        string unit = research.ModifierType == ResearchModifierType.AttackCooldownPercent ||
            research.ModifierType == ResearchModifierType.DurationFlat ? "초" : "";
        return $"{value:0.##}{unit}";
    }

    private void RefreshStatRows()
    {
        int index = 0;
        if (selectedTower != null)
        {
            TowerData data = selectedTower;
            SetStatRow(index++, "공격력", data.baseStats.damage.ToString(), ResearchStatResolver.GetTowerDamage(data.id, data.baseStats.damage).ToString());
            SetStatRow(index++, "공격 간격", $"{data.baseStats.attackCooldown:0.##}초", $"{ResearchStatResolver.GetTowerAttackCooldown(data.id, data.baseStats.attackCooldown):0.##}초");
            SetStatRow(index++, "사거리", $"{data.baseStats.attackRange:0.##}", $"{ResearchStatResolver.GetTowerAttackRange(data.id, data.baseStats.attackRange):0.##}");
        }
        else if (selectedSkill != null)
        {
            SkillData data = selectedSkill;
            if (data.Damage > 0) SetStatRow(index++, "피해량", data.Damage.ToString(), ResearchStatResolver.GetSkillDamage(data).ToString());
            if (data.PeriodicDamage > 0) SetStatRow(index++, "지속 피해", $"{data.PeriodicDamage} / {data.PeriodicInterval:0.##}초", $"{ResearchStatResolver.GetSkillPeriodicDamage(data)} / {data.PeriodicInterval:0.##}초");
            SetStatRow(index++, "범위", $"{data.Range:0.##}", $"{data.Range:0.##}");
            SetStatRow(index++, "재사용 대기시간", $"{data.Cooldown:0.##}초", $"{data.Cooldown:0.##}초");
            if (data.Duration > 0) SetStatRow(index++, "지속 시간", $"{data.Duration:0.##}초", $"{ResearchStatResolver.GetSkillDuration(data):0.##}초");
            if (data.MaxTargets > 0) SetStatRow(index++, "최대 대상", data.MaxTargets.ToString(), data.MaxTargets.ToString());
        }
        for (; index < statRows.Length; index++) statRows[index].Root.gameObject.SetActive(false);
    }

    private void SetStatRow(int index, string name, string baseValue, string currentValue)
    {
        ResearchStatRowView row = statRows[index];
        row.Root.gameObject.SetActive(true);
        row.Name.text = name;
        row.Base.text = baseValue;
        row.Current.text = currentValue;
        row.Current.color = baseValue == currentValue ? new Color(0.9f, 0.91f, 0.89f) : AccentColor;
    }

    private void SetNotice(string message)
    {
        if (noticeText != null)
            noticeText.text = message;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.SetParent(transform, false);
    }

    private TextMeshProUGUI CreateText(
        string objectName, Transform parent, string text, float fontSize,
        TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax)
        => view.Label(objectName, parent, text, fontSize, alignment, color, anchorMin, anchorMax);

    private Button CreateButton(string objectName, Transform parent, string text, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
        => view.Button(objectName, parent, text, color, EdgeColor, anchorMin, anchorMax);

    private Image CreateIcon(string objectName, Transform parent, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax)
        => view.Icon(objectName, parent, sprite, anchorMin, anchorMax);

    private RectTransform CreatePanel(string objectName, Transform parent, Color color)
        => view.Panel(objectName, parent, color);

}
