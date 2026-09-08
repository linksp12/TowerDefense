using UnityEngine;

/// <summary>
/// 피격 위치에 피해 숫자를 표시하고, 위로 이동하며 사라지게 합니다.
/// UI Canvas와 독립적으로 화면 최상단에 그려집니다.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private static readonly Vector3[] PanelWorldCorners = new Vector3[4];

    [SerializeField] private float lifetime = 0.65f;
    [SerializeField] private float risePixels = 42f;

    private Camera worldCamera;
    private Vector3 worldPosition;
    private string damageText;
    private Color damageColor = Color.white;
    private bool isCritical;
    private float elapsed;

    public static void Show(Vector3 hitPosition, int damage)
    {
        Show(hitPosition, damage, false);
    }

    public static void Show(
        Vector3 hitPosition,
        int damage,
        bool critical)
    {
        ShowText(
            hitPosition,
            critical ? $"CRITICAL!\n{damage}" : damage.ToString(),
            critical ? new Color32(255, 190, 35, 255) : GetDamageColor(damage),
            critical
        );
    }

    public static void ShowShield(Vector3 hitPosition)
    {
        ShowText(hitPosition, "Shield", Color.white, false);
    }

    private static void ShowText(
        Vector3 hitPosition,
        string text,
        Color color,
        bool critical)
    {
        if (IsGloballyBlocked())
            return;

        Camera camera = Camera.main;
        if (camera == null)
            camera = FindFirstObjectByType<Camera>();

        if (camera == null)
            return;

        GameObject popupObject = new GameObject("DamagePopup");
        DamagePopup popup = popupObject.AddComponent<DamagePopup>();
        popup.worldCamera = camera;
        popup.worldPosition = hitPosition + Vector3.up * 0.55f;
        popup.isCritical = critical;
        popup.damageText = text;
        popup.damageColor = color;
    }

    private static Color GetDamageColor(int damage)
    {
        if (damage >= 200)
            return new Color32(255, 55, 45, 255);

        if (damage >= 100)
            return new Color32(255, 140, 35, 255);

        if (damage >= 50)
            return new Color32(255, 225, 55, 255);

        return Color.white;
    }

    public static void HideAll()
    {
        DamagePopup[] activePopups =
            FindObjectsByType<DamagePopup>(FindObjectsInactive.Include);

        foreach (DamagePopup popup in activePopups)
        {
            if (popup == null)
                continue;

            popup.enabled = false;
            Destroy(popup.gameObject);
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }

    private void OnGUI()
    {
        if (worldCamera == null || IsGloballyBlocked())
            return;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z < 0f)
            return;

        float progress = Mathf.Clamp01(elapsed / lifetime);
        float y = Screen.height - screenPosition.y - (risePixels * progress);
        float width = isCritical ? 220f : 140f;
        float height = isCritical ? 90f : 60f;
        Rect rect = new Rect(
            screenPosition.x - (width * 0.5f),
            y - (height * 0.5f),
            width,
            height
        );

        if (OverlapsTowerPanel(rect))
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = isCritical ? 34 : 30,
            fontStyle = FontStyle.Bold
        };
        Color fadingColor = damageColor;
        fadingColor.a = 1f - progress;
        style.normal.textColor = fadingColor;

        GUI.Label(rect, damageText, style);
    }

    private static bool IsGloballyBlocked()
    {
        return Time.timeScale <= 0f;
    }

    private static bool OverlapsTowerPanel(Rect popupRect)
    {
        if (TowerBuildManager.Instance != null &&
            TowerBuildManager.Instance.IsOpen &&
            OverlapsPanel(popupRect, TowerBuildManager.Instance.towerBuildPanel))
        {
            return true;
        }

        if (TowerUpgradeUI.Instance != null &&
            TowerUpgradeUI.Instance.IsOpen &&
            OverlapsPanel(popupRect, TowerUpgradeUI.Instance.panel))
        {
            return true;
        }

        return false;
    }

    private static bool OverlapsPanel(Rect popupRect, GameObject panel)
    {
        if (panel == null || !panel.activeInHierarchy)
            return false;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null)
            return false;

        Canvas panelCanvas = panel.GetComponentInParent<Canvas>();
        Camera uiCamera = null;

        if (panelCanvas != null &&
            panelCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = panelCanvas.worldCamera;
        }

        panelRect.GetWorldCorners(PanelWorldCorners);

        Vector2 firstCorner = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            PanelWorldCorners[0]
        );

        float minX = firstCorner.x;
        float maxX = firstCorner.x;
        float minY = firstCorner.y;
        float maxY = firstCorner.y;

        for (int i = 1; i < PanelWorldCorners.Length; i++)
        {
            Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                PanelWorldCorners[i]
            );

            minX = Mathf.Min(minX, screenCorner.x);
            maxX = Mathf.Max(maxX, screenCorner.x);
            minY = Mathf.Min(minY, screenCorner.y);
            maxY = Mathf.Max(maxY, screenCorner.y);
        }

        Rect panelScreenRect = new Rect(
            minX,
            Screen.height - maxY,
            maxX - minX,
            maxY - minY
        );

        return popupRect.Overlaps(panelScreenRect, true);
    }
}
