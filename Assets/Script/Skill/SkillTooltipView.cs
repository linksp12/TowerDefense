using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal struct SkillTooltipStyle
{
    public TMP_FontAsset Font;
    public Sprite Background;
    public Color BackgroundColor, TextColor;
    public float FontSize, Margin;
    public Vector2 Size;
}

/// <summary>Canvas별 공용 툴팁을 표시합니다. 스킬 선택과 능력치 계산은 담당하지 않습니다.</summary>
internal sealed class SkillTooltipView
{
    private static readonly Dictionary<Canvas, SkillTooltipView> Views = new Dictionary<Canvas, SkillTooltipView>();
    private readonly Canvas canvas;
    private readonly GameObject root;
    private readonly RectTransform rect;
    private readonly Image background;
    private readonly TextMeshProUGUI text;
    private readonly Vector3[] corners = new Vector3[4];
    private Object owner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetViews() => Views.Clear();

    public static SkillTooltipView Get(Canvas canvas)
    {
        if (canvas == null) return null;
        List<Canvas> expired = null;
        foreach (Canvas key in Views.Keys)
        {
            if (key != null) continue;
            if (expired == null) expired = new List<Canvas>();
            expired.Add(key);
        }
        if (expired != null)
            foreach (Canvas key in expired) Views.Remove(key);

        if (!Views.TryGetValue(canvas, out SkillTooltipView view) || view.root == null)
        {
            view = new SkillTooltipView(canvas);
            Views[canvas] = view;
        }
        return view;
    }

    private SkillTooltipView(Canvas canvas)
    {
        this.canvas = canvas;
        root = new GameObject("SkillTooltip", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        background = root.GetComponent<Image>();
        background.raycastTarget = false;
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject label = new GameObject("TooltipText", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 16f);
        labelRect.offsetMax = new Vector2(-20f, -16f);
        text = label.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        root.SetActive(false);
    }

    public void Show(Object nextOwner, RectTransform anchor, string content, SkillTooltipStyle style)
    {
        if (root == null || canvas == null || anchor == null) return;
        owner = nextOwner;
        background.sprite = style.Background;
        background.type = style.Background != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = style.BackgroundColor;
        text.font = style.Font != null ? style.Font : TMP_Settings.defaultFontAsset;
        text.text = content;
        text.fontSize = style.FontSize;
        text.color = style.TextColor;
        rect.sizeDelta = style.Size;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Position(anchor, style.Margin);
    }

    public void Hide(Object requester)
    {
        if (owner != null && owner != requester) return;
        if (root != null) root.SetActive(false);
        owner = null;
    }

    private void Position(RectTransform anchor, float margin)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        anchor.GetWorldCorners(corners);
        Vector2 top = RectTransformUtility.WorldToScreenPoint(camera, (corners[1] + corners[2]) * 0.5f);
        Vector2 bottom = RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[3]) * 0.5f);
        float width = rect.rect.width;
        float height = rect.rect.height;
        float y = top.y + height * 0.5f + margin;
        if (top.y + height + margin > Screen.height) y = bottom.y - height * 0.5f - margin;
        Vector2 screenPoint = new Vector2(
            Mathf.Clamp(top.x, width * 0.5f + margin, Screen.width - width * 0.5f - margin),
            Mathf.Clamp(y, height * 0.5f + margin, Screen.height - height * 0.5f - margin));
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camera, out Vector2 local))
            rect.anchoredPosition = local;
    }
}
