using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSpeedController : MonoBehaviour
{
    private static GameSpeedController instance;
    private static int selectedSpeed = 1;

    private TextMeshProUGUI speedText;

    public static void Create(Canvas parentCanvas, float initialSpeed = 1f)
    {
        if (instance != null || parentCanvas == null)
            return;

        selectedSpeed = Mathf.Clamp(Mathf.RoundToInt(initialSpeed), 1, 3);

        GameObject buttonObject = new GameObject(
            "GameSpeedButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(GameSpeedController)
        );
        buttonObject.layer = LayerMask.NameToLayer("UI");
        buttonObject.transform.SetParent(parentCanvas.transform, false);
        buttonObject.transform.SetAsLastSibling();

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-155f, -16f);
        buttonRect.sizeDelta = new Vector2(92f, 50f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.055f, 0.075f, 0.11f, 0.94f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        instance = buttonObject.GetComponent<GameSpeedController>();
        instance.CreateLabel(buttonObject.transform);
        button.onClick.AddListener(instance.CycleSpeed);
        instance.ApplySelectedSpeed();
    }

    public static void ResumeSelectedSpeed()
    {
        Time.timeScale = selectedSpeed;
    }

    private void CreateLabel(Transform parent)
    {
        GameObject textObject = new GameObject(
            "SpeedText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        speedText = textObject.GetComponent<TextMeshProUGUI>();
        speedText.font = FindMaplestoryFont();
        speedText.fontSize = 27f;
        speedText.fontStyle = FontStyles.Bold;
        speedText.color = new Color(1f, 0.74f, 0.2f, 1f);
        speedText.alignment = TextAlignmentOptions.Center;
        speedText.raycastTarget = false;
        speedText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        speedText.outlineWidth = 0.15f;
    }

    private void CycleSpeed()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded())
            return;

        selectedSpeed = selectedSpeed >= 3 ? 1 : selectedSpeed + 1;
        ApplySelectedSpeed();
    }

    private void ApplySelectedSpeed()
    {
        if (Time.timeScale > 0f)
            Time.timeScale = selectedSpeed;

        if (speedText != null)
            speedText.text = $"{selectedSpeed}x";
    }

    private static TMP_FontAsset FindMaplestoryFont()
    {
        TextMeshProUGUI[] sceneTexts =
            FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);

        foreach (TextMeshProUGUI sceneText in sceneTexts)
        {
            TMP_FontAsset font = sceneText != null ? sceneText.font : null;

            if (font != null && font.name == "Maplestory Bold SDF")
                return font;
        }

        Debug.LogError(
            "GameSpeedController: Maplestory Bold SDF 폰트를 찾지 못했습니다."
        );
        return TMP_Settings.defaultFontAsset;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
