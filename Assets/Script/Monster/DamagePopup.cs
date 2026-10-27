using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 피격 위치에 피해 숫자를 표시하고, 튀어 오른 뒤 위로 떠오르며 사라지게 합니다.
/// 전투 UI 캔버스보다 아래 순서의 전용 캔버스에 그려 타워 설치·강화 창을 가리지 않습니다.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private const int MaxPooled = 48;
    private static readonly Stack<DamagePopup> Pool = new Stack<DamagePopup>();
    private static Canvas popupCanvas;
    private static RectTransform canvasRect;
    private static Material outlineMaterial;

    private TextMeshProUGUI label;
    private RectTransform rect;
    private Camera worldCamera;
    private Vector3 worldPosition;
    private Vector2 drift;
    private float elapsed;
    private float lifetime;
    private float rise;
    private float startScale;
    private Color color;

    public static void Show(Vector3 hitPosition, int damage)
    {
        Show(hitPosition, damage, false);
    }

    public static void Show(Vector3 hitPosition, int damage, bool critical)
    {
        if (critical)
            Spawn(hitPosition, $"<size=52%>치명타!</size>\n{damage}", new Color32(255, 214, 92, 255), 44f, 0.9f, 60f, 1.9f, true);
        else
            Spawn(hitPosition, damage.ToString(), GetDamageColor(damage), 32f, 0.7f, 46f, 1.45f, false);
    }

    // 처치 보상: 피해 숫자보다 조금 위에 금색 '+골드'를 띄운다.
    public static void ShowGold(Vector3 position, int amount)
    {
        Spawn(position + Vector3.up * 0.75f, $"+{amount}", new Color32(255, 214, 92, 255), 26f, 0.8f, 44f, 1.35f, false);
    }

    public static void ShowShield(Vector3 hitPosition)
    {
        Spawn(hitPosition, "막힘", new Color32(159, 216, 255, 255), 26f, 0.65f, 38f, 1.3f, false);
    }

    private static Color GetDamageColor(int damage)
    {
        if (damage >= 200)
            return new Color32(255, 85, 64, 255);

        if (damage >= 100)
            return new Color32(255, 152, 56, 255);

        if (damage >= 50)
            return new Color32(255, 226, 92, 255);

        return new Color32(255, 250, 240, 255);
    }

    public static void HideAll()
    {
        // 풀에 들어간 비활성 항목은 건드리지 않는다.
        foreach (DamagePopup popup in FindObjectsByType<DamagePopup>(FindObjectsInactive.Exclude))
        {
            if (popup != null)
                popup.Release();
        }
    }

    private static void Spawn(Vector3 hitPosition, string text, Color color, float fontSize, float lifetime, float rise,
        float startScale, bool critical)
    {
        if (Time.timeScale <= 0f)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            camera = FindAnyObjectByType<Camera>();
        if (camera == null || !EnsureCanvas())
            return;

        DamagePopup popup = Pool.Count > 0 ? Pool.Pop() : Create();
        popup.worldCamera = camera;
        popup.worldPosition = hitPosition + Vector3.up * 0.55f;
        // 여러 타격이 겹쳐도 읽히도록 옆으로 조금씩 흩어 띄운다.
        popup.drift = new Vector2(Random.Range(-16f, 16f), 0f);
        popup.elapsed = 0f;
        popup.lifetime = lifetime;
        popup.rise = rise;
        popup.startScale = startScale;
        popup.color = color;
        popup.label.text = text;
        popup.label.fontSize = fontSize;
        popup.label.color = color;
        popup.label.enableVertexGradient = critical;
        if (critical)
            popup.label.colorGradient = new VertexGradient(new Color32(255, 246, 196, 255), new Color32(255, 246, 196, 255),
                new Color32(255, 138, 40, 255), new Color32(255, 138, 40, 255));
        popup.transform.SetAsLastSibling();
        popup.gameObject.SetActive(true);
        popup.UpdateVisual();
    }

    private static bool EnsureCanvas()
    {
        if (popupCanvas != null)
            return true;

        // 이전 씬의 캔버스와 함께 파괴된 풀 항목은 버린다.
        Pool.Clear();
        var root = new GameObject("DamagePopupCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.layer = LayerMask.NameToLayer("UI");
        popupCanvas = root.GetComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        popupCanvas.sortingOrder = -10;
        // 전투 캔버스와 같이 해상도와 무관한 고정 픽셀 크기로 그린다.
        root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasRect = root.GetComponent<RectTransform>();

        TMP_FontAsset font = BattleUiSkin.Font;
        if (outlineMaterial == null && font != null && font.material != null)
        {
            // 모든 숫자가 같은 외곽선 재질을 공유해 재질 인스턴스가 늘지 않게 한다.
            outlineMaterial = new Material(font.material) { name = "DamagePopupOutline" };
            outlineMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            outlineMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.2f);
            outlineMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.4f);
            outlineMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(20, 10, 6, 255));
            outlineMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            outlineMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.7f));
            outlineMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1f);
            outlineMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.4f);
            outlineMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            ShaderUtilities.UpdateShaderRatios(outlineMaterial);
        }
        return true;
    }

    private static DamagePopup Create()
    {
        var go = new GameObject("DamagePopup", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(DamagePopup));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(popupCanvas.transform, false);
        var popup = go.GetComponent<DamagePopup>();
        popup.rect = go.GetComponent<RectTransform>();
        popup.rect.sizeDelta = new Vector2(240f, 110f);
        popup.label = go.GetComponent<TextMeshProUGUI>();
        popup.label.font = BattleUiSkin.Font;
        if (outlineMaterial != null)
            popup.label.fontSharedMaterial = outlineMaterial;
        popup.label.alignment = TextAlignmentOptions.Center;
        popup.label.lineSpacing = -18f;
        popup.label.raycastTarget = false;
        popup.label.textWrappingMode = TextWrappingModes.NoWrap;
        return popup;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime || worldCamera == null)
        {
            Release();
            return;
        }
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // 일시정지 중에는 숨긴다.
        label.enabled = Time.timeScale > 0f;

        Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
        if (screen.z < 0f ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
        {
            label.enabled = false;
            return;
        }

        float t = Mathf.Clamp01(elapsed / lifetime);
        float riseT = 1f - Mathf.Pow(1f - t, 3f);
        rect.anchoredPosition = local + drift * riseT + Vector2.up * (rise * riseT);

        // 처음 0.14초 동안 크게 튀어나왔다가 제자리 크기로 돌아온다.
        float pop = Mathf.Clamp01(elapsed / 0.14f);
        float scale = Mathf.Lerp(startScale, 1f, 1f - Mathf.Pow(1f - pop, 2f));
        rect.localScale = Vector3.one * scale;

        label.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
    }

    private void Release()
    {
        if (this == null || !gameObject.activeSelf)
            return;
        gameObject.SetActive(false);
        if (Pool.Count < MaxPooled && popupCanvas != null && transform.parent == popupCanvas.transform)
            Pool.Push(this);
        else
            Destroy(gameObject);
    }
}
