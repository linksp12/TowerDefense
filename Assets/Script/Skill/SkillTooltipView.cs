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
    public string Hotkey;   // 단축키 글자(Q·W·E·R), 없으면 비움
}

/// <summary>
/// Canvas별 공용 스킬 설명창(도트 틀). 위: 아이콘·이름·속성 표식 / 가운데: 설명 / 아래: 능력치 칸과 사용 상태.
/// 스킬 선택과 능력치 계산은 담당하지 않는다(문구·수치는 SkillDescriptionFormatter).
/// </summary>
internal sealed class SkillTooltipView
{
    private const float Pad = 22f, IconSize = 66f, RowHeight = 30f;
    private static readonly Color Gold = new Color32(255, 214, 120, 255);
    private static readonly Color Ivory = new Color32(244, 236, 220, 255);
    private static readonly Color Muted = new Color32(176, 164, 144, 255);

    private static readonly Dictionary<Canvas, SkillTooltipView> Views = new Dictionary<Canvas, SkillTooltipView>();
    private readonly Canvas canvas;
    private readonly GameObject root;
    private readonly RectTransform rect;
    private readonly Image icon, tagIcon, separator;
    private readonly TextMeshProUGUI title, tag, body, status;
    private readonly RectTransform stats;
    private readonly List<(Image icon, TextMeshProUGUI label, TextMeshProUGUI value)> cells =
        new List<(Image, TextMeshProUGUI, TextMeshProUGUI)>();
    private readonly SkillTooltipFade fade;
    private readonly Vector3[] corners = new Vector3[4];
    private Object owner;
    private TMP_FontAsset font;
    private float lastRemaining = -1f;
    private string hotkey;

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

    private static Sprite Icon(string name) => Resources.Load<Sprite>("UI/TowerPanel/" + name);

    private SkillTooltipView(Canvas canvas)
    {
        this.canvas = canvas;
        root = new GameObject("SkillTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        var frame = root.GetComponent<Image>();
        frame.raycastTarget = false;
        if (!BattleUiSkin.Skin(frame, BattleUiSkin.Frame, 1f))
            frame.color = new Color(0.08f, 0.06f, 0.05f, 0.96f);
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        fade = root.AddComponent<SkillTooltipFade>();

        // 아이콘(안쪽 틀)
        var inset = Child<Image>("IconFrame", root.transform, Pad, Pad, IconSize, IconSize);
        if (!BattleUiSkin.Skin(inset, BattleUiSkin.Inset, 1f)) inset.color = new Color(0f, 0f, 0f, 0.5f);
        icon = Child<Image>("Icon", inset.transform, 6f, 6f, IconSize - 12f, IconSize - 12f);
        icon.preserveAspect = true;

        float textX = Pad + IconSize + 16f;
        title = Text("Title", textX, Pad + 2f, 260f, 36f, 28f, Gold);
        tagIcon = Child<Image>("TagIcon", root.transform, textX, Pad + 42f, 22f, 22f);
        tagIcon.preserveAspect = true;
        tag = Text("Tag", textX + 28f, Pad + 40f, 240f, 26f, 18f, Muted);

        separator = Child<Image>("Separator", root.transform, Pad, 0f, 0f, 3f);
        separator.sprite = UiFx.Pixel;
        separator.color = new Color(0.86f, 0.64f, 0.3f, 0.55f);

        body = Text("Body", Pad, 0f, 0f, 0f, 19f, Ivory);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.lineSpacing = 6f;

        stats = Child<RectTransform>("Stats", root.transform, Pad, 0f, 0f, 0f);
        status = Text("Status", Pad, 0f, 0f, 26f, 17f, Muted);
        status.richText = true;
        root.SetActive(false);
    }

    // 왼쪽 위 기준 배치(x, y는 틀 왼쪽 위에서 오른쪽·아래로)
    private T Child<T>(string name, Transform parent, float x, float y, float width, float height) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(width, height);
        if (typeof(T) == typeof(RectTransform)) return r as T;
        var component = go.AddComponent(typeof(T)) as T;
        if (component is Graphic graphic) graphic.raycastTarget = false;
        return component;
    }

    private TextMeshProUGUI Text(string name, float x, float y, float width, float height, float size, Color color,
        Transform parent = null)
    {
        var text = Child<TextMeshProUGUI>(name, parent != null ? parent : root.transform, x, y, width, height);
        text.font = font != null ? font : BattleUiSkin.Font;   // 외곽선 재질보다 글꼴이 먼저 있어야 한다
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.12f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }

    private static void Place(RectTransform r, float x, float y, float width, float height)
    {
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(width, height);
    }

    public void Show(Object nextOwner, RectTransform anchor, SkillData skill, SkillTooltipStyle style)
    {
        if (root == null || canvas == null || anchor == null || skill == null) return;
        owner = nextOwner;
        // 새 능력치 칸의 글자가 초기화되도록 먼저 켠다(꺼진 부모 아래에서 만든 TMP는 재질이 없다)
        root.SetActive(true);
        font = style.Font != null ? style.Font : BattleUiSkin.Font;
        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true)) t.font = font;

        RectTransform bounds = (RectTransform)canvas.transform;
        float width = Mathf.Min(Mathf.Max(420f, style.Size.x), bounds.rect.width - 24f);
        float inner = width - Pad * 2f;

        icon.sprite = skill.Icon;
        icon.enabled = skill.Icon != null;
        title.text = SkillDescriptionFormatter.Name(skill);
        var (tagSprite, tagText, tagColor) = SkillDescriptionFormatter.Kind(skill);
        tagIcon.sprite = Icon(tagSprite);
        tagIcon.enabled = tagIcon.sprite != null;
        tag.text = tagText;
        tag.color = tagColor;

        float y = Pad + IconSize + 14f;
        Place(separator.rectTransform, Pad, y, inner, 3f);
        y += 13f;

        body.text = SkillDescriptionFormatter.Effect(skill);
        float bodyHeight = body.GetPreferredValues(body.text, inner, Mathf.Infinity).y;
        Place(body.rectTransform, Pad, y, inner, bodyHeight);
        y += bodyHeight + 14f;

        // 능력치 칸: 두 줄씩 배치
        var list = SkillDescriptionFormatter.Stats(skill);
        float cellWidth = (inner - 12f) / 2f;
        for (int i = 0; i < list.Count; i++)
        {
            if (i >= cells.Count) cells.Add(NewCell());
            var cell = cells[i];
            cell.icon.transform.parent.gameObject.SetActive(true);
            var cellRect = (RectTransform)cell.icon.transform.parent;
            Place(cellRect, (i % 2) * (cellWidth + 12f), (i / 2) * (RowHeight + 6f), cellWidth, RowHeight);
            cell.icon.sprite = Icon(list[i].icon);
            cell.icon.enabled = cell.icon.sprite != null;
            cell.label.text = list[i].label;
            cell.value.text = list[i].value;
            cell.label.font = cell.value.font = font;
        }
        for (int i = list.Count; i < cells.Count; i++)
            cells[i].icon.transform.parent.gameObject.SetActive(false);
        float statsHeight = ((list.Count + 1) / 2) * (RowHeight + 6f);
        Place(stats, Pad, y, inner, statsHeight);
        y += statsHeight + 6f;

        Place(status.rectTransform, Pad, y, inner, 26f);
        hotkey = style.Hotkey;
        lastRemaining = -1f;
        RefreshStatus(nextOwner, skill);
        y += 26f + Pad - 4f;

        rect.sizeDelta = new Vector2(width, y);
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Position(anchor, style.Margin);
        fade.Play();
    }

    private (Image, TextMeshProUGUI, TextMeshProUGUI) NewCell()
    {
        var cell = Child<Image>("Stat", stats, 0f, 0f, 0f, RowHeight);
        cell.sprite = UiFx.Pixel;
        cell.color = new Color(0f, 0f, 0f, 0.32f);
        var cellIcon = Child<Image>("Icon", cell.transform, 6f, 4f, 22f, 22f);
        cellIcon.preserveAspect = true;
        var label = Text("Label", 34f, 5f, 110f, 22f, 16f, Muted, cell.transform);
        var value = Text("Value", 0f, 4f, 0f, 24f, 19f, Ivory, cell.transform);
        var valueRect = value.rectTransform;
        valueRect.anchorMin = new Vector2(0f, 1f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.pivot = new Vector2(1f, 1f);
        valueRect.offsetMin = new Vector2(0f, -28f);
        valueRect.offsetMax = new Vector2(-8f, -4f);
        value.alignment = TextAlignmentOptions.TopRight;
        return (cellIcon, label, value);
    }

    /// <summary>아래 줄 사용 상태(재사용까지 남은 시간). 보이는 동안 버튼이 매 프레임 불러도 값이 바뀔 때만 갱신한다.</summary>
    public void RefreshStatus(Object requester, SkillData skill)
    {
        if (root == null || !ReferenceEquals(owner, requester) || skill == null) return;
        float remaining = SkillManager.Instance != null ? SkillManager.Instance.GetCooldownRemaining(skill.SkillId) : 0f;
        float shown = Mathf.Ceil(remaining);
        if (Mathf.Approximately(shown, lastRemaining)) return;
        lastRemaining = shown;
        string key = string.IsNullOrEmpty(hotkey) ? "클릭" : $"<color=#FFD678>{hotkey}</color>·클릭";
        status.text = remaining > 0f
            ? $"<color=#FF9A6A>재사용까지 {shown:0}초</color>"
            : $"<color=#8CE878>● 사용 가능</color>  <color=#B0A490>{key} → 위치 지정 · 우클릭 취소</color>";
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

/// <summary>설명창이 뜰 때 살짝 떠오르며 나타난다(시간 정지 중에도 동작).</summary>
internal sealed class SkillTooltipFade : MonoBehaviour
{
    private CanvasGroup group;
    private float startedAt;
    private Vector3 basePosition;

    public void Play()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        startedAt = Time.unscaledTime;
        basePosition = transform.localPosition;
        group.alpha = 0f;
        enabled = true;
    }

    private void Update()
    {
        float k = Mathf.Clamp01((Time.unscaledTime - startedAt) / 0.14f);
        float eased = UiFx.EaseOut(k);
        group.alpha = eased;
        transform.localPosition = basePosition + new Vector3(0f, -8f * (1f - eased), 0f);
        if (k >= 1f) enabled = false;
    }
}
