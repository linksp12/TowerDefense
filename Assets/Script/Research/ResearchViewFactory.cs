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
