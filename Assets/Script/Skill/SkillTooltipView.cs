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
        text.overflowMode = TextOverflowModes.Ellipsis;
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
        text.fontSize = Mathf.Max(14f, style.FontSize);
        text.color = style.TextColor;
        RectTransform bounds = (RectTransform)canvas.transform;
        float width = Mathf.Min(style.Size.x, bounds.rect.width - 24f);
        float height = text.GetPreferredValues(content, width - 40f, Mathf.Infinity).y + 32f;
        float maximum = bounds.rect.height - 100f;
        while (height > maximum && text.fontSize > 12f)
        {
            text.fontSize -= 1f;
            height = text.GetPreferredValues(content, width - 40f, Mathf.Infinity).y + 32f;
        }
        rect.sizeDelta = new Vector2(width, Mathf.Min(height, maximum));
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
        anchor.GetWorldCorners(corners);
        Vector3 top = canvasRect.InverseTransformPoint((corners[1] + corners[2]) * 0.5f);
        Vector3 bottom = canvasRect.InverseTransformPoint((corners[0] + corners[3]) * 0.5f);
        float width = rect.rect.width;
        float height = rect.rect.height;
        float y = top.y + height * 0.5f + margin;
        Rect area = canvasRect.rect;
        if (top.y + height + margin > area.yMax) y = bottom.y - height * 0.5f - margin;
        rect.localPosition = new Vector3(
            Mathf.Clamp(top.x, area.xMin + width * 0.5f + 8f, area.xMax - width * 0.5f - 8f),
            Mathf.Clamp(y, area.yMin + height * 0.5f + 8f, area.yMax - height * 0.5f - 8f), 0);
    }
}
