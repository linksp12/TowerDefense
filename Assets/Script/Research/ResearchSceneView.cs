using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>씬에 배치된 연구소 UI를 표시합니다. 저장과 능력치 계산은 컨트롤러에 맡깁니다.</summary>
public sealed class ResearchSceneView : MonoBehaviour
{
    [Header("목록과 프리팹")]
    [SerializeField] private RectTransform towerContent;
    [SerializeField] private RectTransform skillContent;
    [SerializeField] private GameObject towerEmpty;
    [SerializeField] private GameObject skillEmpty;
    [SerializeField] private ResearchTargetCardView targetCardPrefab;
    [SerializeField] private ResearchOptionButtonView optionButtonPrefab;
    [SerializeField] private RectTransform researchOptions;
    [Header("상세 정보")]
    [SerializeField] private TMP_Text skillPoints;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailCategory;
    [SerializeField] private TMP_Text detailTitle;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text purchaseInfo;
    [SerializeField] private TMP_Text purchaseLabel;
    [SerializeField] private TMP_Text notice;
    [Header("능력치 행: 필요한 만큼 추가하고 재사용")]
    [SerializeField] private ScrollRect statsScroll;
    [SerializeField] private ResearchStatRowView[] initialStatRows = Array.Empty<ResearchStatRowView>();
    [SerializeField] private ResearchStatRowView statRowTemplate;
    [Header("버튼과 오디오")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Image purchaseBackground;
    [SerializeField] private Outline purchaseOutline;
    [SerializeField] private AudioSource buttonAudio;
    [Header("개발용 도구")]
    [SerializeField] private GameObject testTools;
    [SerializeField] private Button addPointButton;
    [SerializeField] private Button resetButton;
    [Header("동적 상태 색")]
    [SerializeField] private ResearchPalette palette = new ResearchPalette();
    private readonly List<ResearchStatRowView> statRows = new List<ResearchStatRowView>();

    public ResearchPalette Palette => palette;
    public AudioSource ButtonAudio => buttonAudio;
    public Button PurchaseButton => purchaseButton;
    public Button BackButton => backButton;
    public Button AddPointButton => addPointButton;
    public Button ResetButton => resetButton;
    public bool TestToolsVisible => testTools != null && testTools.activeSelf;
    public string HighlightHex => ColorUtility.ToHtmlStringRGB(palette.Highlight);
    public string HintHex => ColorUtility.ToHtmlStringRGB(palette.Hint);

    public bool Initialize()
    {
        SetTestToolsVisible(false);
        var missing = new List<string>();
        Require(towerContent, nameof(towerContent), missing); Require(skillContent, nameof(skillContent), missing);
        Require(towerEmpty, nameof(towerEmpty), missing); Require(skillEmpty, nameof(skillEmpty), missing);
        Require(targetCardPrefab, nameof(targetCardPrefab), missing); Require(optionButtonPrefab, nameof(optionButtonPrefab), missing);
        Require(researchOptions, nameof(researchOptions), missing); Require(skillPoints, nameof(skillPoints), missing);
        Require(detailIcon, nameof(detailIcon), missing); Require(detailCategory, nameof(detailCategory), missing);
        Require(detailTitle, nameof(detailTitle), missing); Require(detailDescription, nameof(detailDescription), missing);
        Require(purchaseInfo, nameof(purchaseInfo), missing); Require(purchaseLabel, nameof(purchaseLabel), missing);
        Require(notice, nameof(notice), missing); Require(purchaseButton, nameof(purchaseButton), missing);
        Require(backButton, nameof(backButton), missing); Require(purchaseBackground, nameof(purchaseBackground), missing);
        Require(purchaseOutline, nameof(purchaseOutline), missing); Require(buttonAudio, nameof(buttonAudio), missing);
        Require(statsScroll, nameof(statsScroll), missing); Require(statRowTemplate, nameof(statRowTemplate), missing);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Require(testTools, nameof(testTools), missing); Require(addPointButton, nameof(addPointButton), missing); Require(resetButton, nameof(resetButton), missing);
#endif
        if (targetCardPrefab != null && !targetCardPrefab.IsConfigured) missing.Add("targetCardPrefab 내부 연결");
        if (optionButtonPrefab != null && !optionButtonPrefab.IsConfigured) missing.Add("optionButtonPrefab 내부 연결");
        if (statRowTemplate != null && !statRowTemplate.IsConfigured) missing.Add("statRowTemplate 내부 연결");
        if (statsScroll != null)
        {
            Require(statsScroll.content, "statsScroll.content", missing);
            Require(statsScroll.viewport, "statsScroll.viewport", missing);
        }
        if (initialStatRows == null || Array.Exists(initialStatRows, x => x == null || !x.IsConfigured)) missing.Add("initialStatRows 내부 연결");
        if (missing.Count > 0)
        {
            Debug.LogError("ResearchSceneView 연결 누락: " + string.Join(", ", missing), this);
            if (purchaseButton != null) purchaseButton.interactable = false;
            return false;
        }
        if (palette == null) palette = new ResearchPalette();
        statRows.Clear();
        statRows.AddRange(initialStatRows);
        foreach (var effect in GetComponentsInChildren<ButtonEffect>(true))
            effect.fallbackAudioSource = buttonAudio;
        return true;
    }

    private static void Require(UnityEngine.Object value, string name, List<string> missing) { if (value == null) missing.Add(name); }
    public ResearchTargetCardView CreateTargetCard(ResearchTargetType type)
        => Instantiate(targetCardPrefab, type == ResearchTargetType.Tower ? towerContent : skillContent);
    public ResearchOptionButtonView CreateOptionButton() => Instantiate(optionButtonPrefab, researchOptions);
    public void SetEmptyLists(bool towersEmpty, bool skillsEmpty) { towerEmpty.SetActive(towersEmpty); skillEmpty.SetActive(skillsEmpty); }
    public void SetSkillPoints(int value) => skillPoints.text = $"스킬 포인트  {value}";
    public void SetNotice(string message) => notice.text = message;
    public void SetTestToolsVisible(bool visible) { if (testTools != null) testTools.SetActive(visible); }

    public void SetTarget(string category, bool isTower, string title, string description, Sprite icon)
    {
        detailCategory.text = category;
        detailCategory.color = isTower ? palette.Tower : palette.Skill;
        detailTitle.text = title;
        detailDescription.text = description;
        detailIcon.sprite = icon;
        detailIcon.enabled = icon != null;
    }

    public void SetPurchaseInfo(string text) => purchaseInfo.text = text;
    public void SetPurchaseAppearance(string label, bool interactable, bool highlighted)
    {
        purchaseLabel.text = label;
        purchaseLabel.color = highlighted ? palette.PurchaseText : palette.Muted;
        purchaseBackground.color = highlighted ? palette.Accent : palette.PurchaseDisabled;
        purchaseOutline.effectColor = highlighted ? palette.PurchaseEdge : palette.Edge;
        purchaseButton.interactable = interactable;
    }

    public void SetStatRow(int index, string name, string baseValue, string currentValue)
    {
        while (statRows.Count <= index)
        {
            var row = Instantiate(statRowTemplate, statsScroll.content);
            row.name = "Stat_" + statRows.Count;
            statRows.Add(row);
        }
        statRows[index].Render(name, baseValue, currentValue, palette.CurrentNeutral, palette.Accent);
    }

    public void SetStatCount(int count)
    {
        for (int i = count; i < statRows.Count; i++) statRows[i].gameObject.SetActive(false);
    }
    public void ResetStatScroll() => statsScroll.verticalNormalizedPosition = 1f;
}

/// <summary>씬 Inspector에서 편집하는 UI 색입니다. 연구 효과 데이터와 분리합니다.</summary>
[Serializable]
public sealed class ResearchPalette
{
    public Color Tower = new Color(0.93f, 0.72f, 0.40f);
    public Color Skill = new Color(0.46f, 0.78f, 0.91f);
    public Color Accent = new Color(0.94f, 0.75f, 0.38f);
    public Color Muted = new Color(0.63f, 0.68f, 0.70f);
    public Color Edge = new Color(0.38f, 0.34f, 0.27f);
    public Color CardBackground = new Color(0.13f, 0.17f, 0.19f);
    public Color SelectedBackground = new Color(0.20f, 0.24f, 0.24f);
    public Color RankOff = new Color(0.24f, 0.29f, 0.30f);
    public Color CurrentNeutral = new Color(0.9f, 0.91f, 0.89f);
    public Color PurchaseText = new Color(0.16f, 0.12f, 0.075f);
    public Color PurchaseDisabled = new Color(0.15f, 0.20f, 0.22f);
    public Color PurchaseEdge = new Color(1f, 0.87f, 0.58f);
    public Color Highlight = new Color32(237, 195, 108, 255);
    public Color Hint = new Color32(165, 173, 176, 255);
}
