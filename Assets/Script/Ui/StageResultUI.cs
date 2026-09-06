using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageResultUI : MonoBehaviour
{
    private static StageResultUI instance;

    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private TMP_FontAsset displayFont;
    private bool hasClickedButton;

    public static void Show(bool victory, string currentSceneName, string nextSceneName)
    {
        if (instance != null)
            return;

        GameObject root = new GameObject(
            "StageResultUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(StageResultUI)
        );

        instance = root.GetComponent<StageResultUI>();
        instance.displayFont = FindKoreanFont();
        instance.Build(victory, currentSceneName, nextSceneName);
    }

    private static TMP_FontAsset FindKoreanFont()
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
            "StageResultUI: Maplestory Bold SDF 폰트를 찾지 못했습니다. " +
            "기본 TMP 폰트를 사용합니다."
        );
        return TMP_Settings.defaultFontAsset;
    }

    private void Build(bool victory, string currentSceneName, string nextSceneName)
    {
        gameObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        RectTransform rootRect = GetComponent<RectTransform>();
        Stretch(rootRect);

        Image dimmedBackground = CreateImage(
            "DimmedBackground",
            transform,
            new Color(0f, 0f, 0f, 0.68f)
        );
        Stretch(dimmedBackground.rectTransform);

        Image panel = CreateImage(
            "ResultPanel",
            transform,
            new Color(0.055f, 0.075f, 0.11f, 0.98f)
        );
        panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(900f, 520f);

        Image topLine = CreateImage(
            "TopLine",
            panel.transform,
            victory
                ? new Color(1f, 0.73f, 0.18f, 1f)
                : new Color(0.78f, 0.2f, 0.18f, 1f)
        );
        RectTransform topLineRect = topLine.rectTransform;
        topLineRect.anchorMin = new Vector2(0f, 1f);
        topLineRect.anchorMax = new Vector2(1f, 1f);
        topLineRect.pivot = new Vector2(0.5f, 1f);
        topLineRect.anchoredPosition = Vector2.zero;
        topLineRect.sizeDelta = new Vector2(0f, 10f);

        CreateText(
            "ResultTitle",
            panel.transform,
            victory ? "스테이지 클리어!" : "전투 실패",
            new Vector2(0f, 145f),
            new Vector2(760f, 90f),
            58f,
            victory
                ? new Color(1f, 0.78f, 0.25f, 1f)
                : new Color(1f, 0.35f, 0.3f, 1f)
        );

        CreateText(
            "ResultMessage",
            panel.transform,
            victory
                ? "모든 웨이브를 막아냈습니다."
                : "왕국이 무너졌습니다. 다시 도전해 보세요.",
            new Vector2(0f, 52f),
            new Vector2(760f, 60f),
            29f,
            new Color(0.9f, 0.92f, 0.96f, 1f)
        );

        if (victory)
        {
            CreateButton(
                panel.transform,
                "메인으로",
                new Vector2(-270f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene("MainScene")
            );
            CreateButton(
                panel.transform,
                "다시하기",
                new Vector2(0f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene(currentSceneName)
            );
            CreateButton(
                panel.transform,
                "다음 스테이지",
                new Vector2(270f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene(nextSceneName),
                true
            );
        }
        else
        {
            CreateButton(
                panel.transform,
                "메인으로",
                new Vector2(-135f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene("MainScene")
            );
            CreateButton(
                panel.transform,
                "다시하기",
                new Vector2(135f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene(currentSceneName),
                true
            );
        }

        panelRect.localScale = Vector3.one * 0.92f;
        StartCoroutine(AnimateIn());
    }

    private System.Collections.IEnumerator AnimateIn()
    {
        const float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);

            canvasGroup.alpha = progress;
            panelRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        panelRect.localScale = Vector3.one;
    }

    private void LoadScene(string sceneName)
    {
        if (hasClickedButton)
            return;

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"StageResultUI: 이동할 씬을 찾을 수 없습니다: {sceneName}");
            return;
        }

        hasClickedButton = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void CreateButton(
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        UnityAction onClick,
        bool highlighted = false
    )
    {
        Image buttonImage = CreateImage(
            label + "Button",
            parent,
            highlighted
                ? new Color(0.82f, 0.52f, 0.12f, 1f)
                : new Color(0.16f, 0.22f, 0.31f, 1f)
        );

        RectTransform buttonRect = buttonImage.rectTransform;
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = size;

        Button button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        CreateText(
            "Label",
            button.transform,
            label,
            Vector2.zero,
            size,
            label.Length >= 6 ? 24f : 28f,
            Color.white
        );
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        string content,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = displayFont;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.outlineColor = new Color(0f, 0f, 0f, 0.75f);
        text.outlineWidth = 0.12f;

        return text;
    }

    private Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );
        imageObject.layer = gameObject.layer;
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
