using UnityEngine;

/// <summary>
/// 피격 위치에 피해 숫자를 표시하고, 위로 이동하며 사라지게 합니다.
/// UI Canvas와 독립적으로 화면 최상단에 그려집니다.
/// </summary>
public class DamagePopup : MonoBehaviour
{
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
        popup.damageText = critical
            ? $"CRITICAL!\n{damage}"
            : damage.ToString();
        popup.damageColor = critical
            ? new Color32(255, 190, 35, 255)
            : GetDamageColor(damage);
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
        if (worldCamera == null)
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
}
