using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>연구소의 시각 요소를 생성합니다. 구매와 능력치 계산은 다루지 않습니다.</summary>
public sealed class ResearchViewFactory
{
    private readonly TMP_FontAsset font;

    public ResearchViewFactory(TMP_FontAsset font) => this.font = font;

    public RectTransform Panel(string name, Transform parent, Color color)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        Image image = root.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return root.GetComponent<RectTransform>();
    }

    public RectTransform Frame(string name, Transform parent, Color color, Color edge,
        Vector2 min, Vector2 max)
    {
        RectTransform root = Panel(name, parent, color);
        Place(root, min, max);
        Shadow shadow = root.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(0f, -7f);
        Outline outline = root.gameObject.AddComponent<Outline>();
        outline.effectColor = edge;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = false;
        return root;
    }

    public Button Button(string name, Transform parent, string text, Color color, Color edge,
        Vector2 min, Vector2 max, float fontSize = 27f, Color? textColor = null)
    {
        RectTransform root = Frame(name, parent, color, edge, min, max);
        Image image = root.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
        colors.disabledColor = new Color(0.8f, 0.8f, 0.8f);
        button.colors = colors;
        if (!string.IsNullOrEmpty(text))
            Label("Label", root, text, fontSize, TextAlignmentOptions.Center,
                textColor ?? Color.white, Vector2.zero, Vector2.one);
        return button;
    }

    public TextMeshProUGUI Label(string name, Transform parent, string text, float size,
        TextAlignmentOptions alignment, Color color, Vector2 min, Vector2 max)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        root.transform.SetParent(parent, false);
        TextMeshProUGUI label = root.GetComponent<TextMeshProUGUI>();
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.fontStyle = FontStyles.Normal;
        label.text = text;
        label.alignment = alignment;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        Place(label.rectTransform, min, max);
        return label;
    }

    public Image Icon(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max)
    {
        RectTransform root = Panel(name, parent, Color.white);
        Place(root, min, max);
        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    public void Rule(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        => Place(Panel(name, parent, color), min, max);

    /// <summary>카드 크기를 유지하는 세로 목록입니다. 내용이 넘칠 때만 스크롤바가 표시됩니다.</summary>
    public RectTransform ScrollList(string name, Transform parent, Vector2 min, Vector2 max, Color accent)
    {
        RectTransform root = Panel(name, parent, Color.clear);
        Place(root, min, max);
        ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport = Panel("Viewport", root, Color.clear);
        Place(viewport, Vector2.zero, Vector2.one);
        viewport.GetComponent<Image>().raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = Panel("Content", viewport, Color.clear);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 20f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform bar = Panel("Scrollbar", root, new Color(0.04f, 0.065f, 0.08f, 0.8f));
        bar.anchorMin = new Vector2(1f, 0f);
        bar.anchorMax = Vector2.one;
        bar.pivot = new Vector2(1f, 0.5f);
        bar.sizeDelta = new Vector2(10f, 0f);
        bar.anchoredPosition = Vector2.zero;
        bar.GetComponent<Image>().raycastTarget = true;
        RectTransform handle = Panel("Handle", bar, accent);
        Place(handle, Vector2.zero, Vector2.one);
        handle.GetComponent<Image>().raycastTarget = true;
        Scrollbar scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing = 12f;
        return content;
    }

    public static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public sealed class ResearchTargetView
{
    public Button Button;
    public TextMeshProUGUI Status;
    public Image[] RankPips;
    public Color Accent;
}

public sealed class ResearchStatRowView
{
    public RectTransform Root;
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Base;
    public TextMeshProUGUI Current;
}
