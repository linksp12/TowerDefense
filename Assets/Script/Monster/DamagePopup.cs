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
    private float elapsed;

    public static void Show(Vector3 hitPosition, int damage)
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
        popup.damageText = damage.ToString();
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
        Rect rect = new Rect(screenPosition.x - 70f, y - 30f, 140f, 60f);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = new Color(1f, 1f, 1f, 1f - progress);

        GUI.Label(rect, damageText, style);
    }
}
