using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 설치 링·업그레이드 창이 함께 쓰는 도트 조각(Resources/UI/TowerPanel)과 배치 도우미.
/// 좌표는 부모 왼쪽 위 기준 도트 단위(아래로 +), 1도트 = 1080p 화면에서 3px.
/// </summary>
internal static class TowerPanelArt
{
    public const float Dot = 3f;
    // 메이플 서체는 같은 크기에서 글자가 작게 보여 조금 키운다.
    private const float FontScale = 1.15f;

    public static readonly Color Ivory = new Color32(245, 235, 211, 255);
    public static readonly Color Gold = new Color32(255, 214, 102, 255);
    public static readonly Color Muted = new Color32(196, 176, 146, 255);
    public static readonly Color Bad = new Color32(255, 120, 96, 255);
    public static readonly Color Good = new Color32(140, 232, 120, 255);
    public static readonly Color Ice = new Color32(150, 215, 255, 255);
    public static readonly Color Fire = new Color32(255, 160, 90, 255);
    public static readonly Color Ink = new Color32(60, 34, 12, 255);
    public static readonly Color KeyInk = new Color32(40, 30, 24, 255);

    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private static Material outlined;
    private static TMP_FontAsset outlinedFont;

    public static Sprite Sprite(string name)
    {
        if (sprites.TryGetValue(name, out Sprite sprite) && sprite != null)
            return sprite;
        sprite = Resources.Load<Sprite>("UI/TowerPanel/" + name);
        if (sprite == null)
            Debug.LogWarning($"TowerPanelArt: UI/TowerPanel/{name} 그림이 없습니다.");
        sprites[name] = sprite;
        return sprite;
    }

    // 전투 Canvas(고정 픽셀 크기) 안에서도 1080p 기준 크기로 보이도록 묶음 하나를 만든다.
    public static RectTransform CreateRoot(string name, Canvas canvas)
    {
        var root = Group(name, canvas.transform, 0f, 0f, 1f, 1f);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.gameObject.AddComponent<CanvasGroup>();
        return root;
    }

    public static void FitRoot(RectTransform root, Canvas canvas)
    {
        var canvasRect = (RectTransform)canvas.transform;
        float scale = Screen.height / 1080f / Mathf.Max(0.0001f, canvas.scaleFactor);
        root.localScale = new Vector3(scale, scale, 1f);
        root.sizeDelta = canvasRect.rect.size / scale;
        root.anchoredPosition = Vector2.zero;
    }

    // 월드 위치 → 묶음 안 좌표(가운데 원점, 위로 +)
    public static Vector2 WorldToRoot(RectTransform root, Vector3 world)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root, camera.WorldToScreenPoint(world), null, out Vector2 local);
        return local;
    }

    // 창(왼쪽 위 기준)을 묶음 가운데 원점 좌표에 둔다.
    public static void PlaceTopLeft(RectTransform rect, Vector2 topLeft)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeft;
    }

    public static RectTransform Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x * Dot, -y * Dot);
        rect.sizeDelta = new Vector2(w * Dot, h * Dot);
        return rect;
    }

    public static RectTransform Group(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return Place(rect, x, y, w, h);
    }

    public static Image Image(string name, Transform parent, string sprite, float x, float y, bool raycast = false)
    {
        Sprite art = Sprite(sprite);
        Vector2 size = art != null ? art.rect.size : new Vector2(8f, 8f);
        var image = Picture(name, parent, x, y, size.x, size.y, false);
        image.sprite = art;
        image.raycastTarget = raycast;
        return image;
    }

    public static Image Picture(string name, Transform parent, float x, float y, float w, float h, bool keepAspect = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        var image = go.GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        Place(image.rectTransform, x, y, w, h);
        image.preserveAspect = keepAspect;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, float x, float y, float w, float h,
        float size, Color color, TextAlignmentOptions align, bool outline = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);
        Place(text.rectTransform, x, y, w, h);
        text.font = BattleUiSkin.Font;
        if (outline)
            text.fontSharedMaterial = OutlinedMaterial(text.font);
        text.fontSize = size * FontScale;
        text.color = color;
        text.alignment = align;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    // 글자 테두리는 재질 하나를 같이 쓴다(글자마다 재질 복제 방지).
    private static Material OutlinedMaterial(TMP_FontAsset font)
    {
        if (outlined != null && outlinedFont == font)
            return outlined;
        outlinedFont = font;
        outlined = new Material(font.material) { name = font.name + " TowerPanelOutline" };
        outlined.EnableKeyword(ShaderUtilities.Keyword_Outline);
        outlined.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
        outlined.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(24, 14, 8, 255));
        return outlined;
    }

    public static Button MakeButton(Image image)
    {
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.82f, 1f);
        colors.pressedColor = new Color(0.82f, 0.74f, 0.58f, 1f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        return button;
    }

    public static void SetSprite(Image image, string sprite)
    {
        Sprite art = Sprite(sprite);
        image.sprite = art;
        if (art != null)
            image.rectTransform.sizeDelta = art.rect.size * Dot;
    }

    // 타워 그림은 투명 여백이 한쪽으로 치우쳐 있어 그림 전체를 맞추면 밀려 보인다.
    // 스프라이트 외곽선(보이는 부분)을 상자 안에 맞춘다. alignBottom이면 받침 위에 세운다. 좌표는 도트.
    public static void FitVisible(Image image, Sprite sprite, float x, float y, float w, float h, bool alignBottom)
    {
        image.sprite = sprite;
        image.preserveAspect = false;
        image.enabled = sprite != null;
        if (sprite == null)
            return;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector2 vertex in sprite.vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }
        float ppu = sprite.pixelsPerUnit;
        Vector2 rectSize = sprite.rect.size / ppu;
        Vector2 pivot = sprite.pivot / ppu;
        Vector2 visibleMin = min + pivot;   // 스프라이트 사각형 왼쪽 아래 기준
        Vector2 visible = max - min;
        if (visible.x <= 0f || visible.y <= 0f)
            return;

        float k = Mathf.Min(w / visible.x, h / visible.y);   // 1유닛 = k도트
        Vector2 size = rectSize * k;
        float left = x + w / 2f - (visibleMin.x + visible.x / 2f) * k;
        float visibleBottom = alignBottom ? y + h : y + h / 2f + visible.y * k / 2f;
        float top = visibleBottom - (size.y - visibleMin.y * k);
        Place(image.rectTransform, left, top, size.x, size.y);
    }

    public static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }
}

/// <summary>칸·카드에 마우스가 올라가고 나갈 때 알려 준다.</summary>
internal sealed class TowerPanelHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public System.Action Entered;
    public System.Action Exited;

    public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();
}
