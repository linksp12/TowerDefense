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

    public static void Show(bool victory, string currentSceneName, int remainingBaseHealth,
        int maxBaseHealth, string unlockedSceneName, bool progressSaveFailed)
    {
        if (instance != null)
            return;

        GameObject root = new GameObject(
            "StageResultUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(StageResultUI)
        );

        instance = root.GetComponent<StageResultUI>();
        instance.displayFont = FindKoreanFont();
        instance.Build(victory, currentSceneName, remainingBaseHealth, maxBaseHealth,
            unlockedSceneName, progressSaveFailed);
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

    private void Build(bool victory, string currentSceneName, int remainingBaseHealth,
        int maxBaseHealth, string unlockedSceneName, bool progressSaveFailed)
    {
        gameObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 최상위 한 단계는 씬 전환 로딩 화면에 남겨 둔다.
        canvas.sortingOrder = short.MaxValue - 1;

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

        UnityEngine.UI.Image dimmedBackground = CreateImage(
            "DimmedBackground",
            transform,
            new Color(0f, 0f, 0f, 0.68f)
        );
        Stretch(dimmedBackground.rectTransform);

        UnityEngine.UI.Image panel = CreateImage(
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

        UnityEngine.UI.Image topLine = CreateImage(
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
            victory ? GetStageLabel(currentSceneName) + " 클리어!" : "전투 실패",
            new Vector2(0f, 145f),
            new Vector2(760f, 90f),
            54f,
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
            new Vector2(0f, 70f),
            new Vector2(760f, 50f),
            29f,
            new Color(0.9f, 0.92f, 0.96f, 1f)
        );

        CreateText(
            "RemainingBaseHealth",
            panel.transform,
            $"남은 기지 체력  {remainingBaseHealth} / {maxBaseHealth}",
            new Vector2(0f, 10f),
            new Vector2(760f, 45f),
            28f,
            new Color(0.9f, 0.92f, 0.96f, 1f)
        );

        if (victory && (progressSaveFailed || !string.IsNullOrEmpty(unlockedSceneName)))
        {
            CreateText(
                "ProgressNotice",
                panel.transform,
                progressSaveFailed
                    ? "클리어 기록을 저장하지 못했습니다. 스테이지 해금을 확인해 주세요."
                    : GetStageLabel(unlockedSceneName) + "가 해금되었습니다!",
                new Vector2(0f, -55f),
                new Vector2(800f, 45f),
                progressSaveFailed ? 23f : 27f,
                progressSaveFailed
                    ? new Color(1f, 0.55f, 0.4f, 1f)
                    : new Color(1f, 0.78f, 0.25f, 1f)
            );
        }

        if (victory)
        {
            CreateButton(
                panel.transform,
                "다시하기",
                new Vector2(-270f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene(currentSceneName)
            );
            CreateButton(
                panel.transform,
                "스테이지 선택",
                new Vector2(0f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene("StageSelectionScene")
            );
            CreateButton(
                panel.transform,
                "광장으로",
                new Vector2(270f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene("PlazaScene"),
                true
            );
        }
        else
        {
            CreateButton(
                panel.transform,
                "다시하기",
                new Vector2(-135f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene(currentSceneName)
            );
            CreateButton(
                panel.transform,
                "광장으로",
                new Vector2(135f, -150f),
                new Vector2(230f, 74f),
                () => LoadScene("PlazaScene"),
                true
            );
        }

        panelRect.localScale = Vector3.one * 0.92f;
        StartCoroutine(AnimateIn());
    }

    private static string GetStageLabel(string sceneName)
    {
        switch (LastOfTheTower.Progression.StageIds.FromSceneName(sceneName))
        {
            case LastOfTheTower.Progression.StageIds.StageOne: return "1스테이지";
            case LastOfTheTower.Progression.StageIds.StageTwo: return "2스테이지";
            case LastOfTheTower.Progression.StageIds.StageThree: return "3스테이지";
            case LastOfTheTower.Progression.StageIds.StageFour: return "4스테이지";
            default: return "스테이지";
        }
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
        if (hasClickedButton || SceneLoadingScreen.IsLoading)
            return;

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"StageResultUI: 이동할 씬을 찾을 수 없습니다: {sceneName}");
            return;
        }

        hasClickedButton = true;
        canvasGroup.interactable = false;
        string message = sceneName == "PlazaScene" ? "광장으로 이동 중" :
            sceneName == "StageSelectionScene" ? "스테이지 선택 화면을 준비하고 있습니다" :
            "전장을 다시 준비하고 있습니다";
        if (!SceneLoadingScreen.TryLoad(sceneName, message, RestoreAfterLoadFailure))
            RestoreAfterLoadFailure();
    }

    private void RestoreAfterLoadFailure()
    {
        if (this == null)
            return;
        hasClickedButton = false;
        canvasGroup.interactable = true;
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
        UnityEngine.UI.Image buttonImage = CreateImage(
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

        UnityEngine.UI.Button button = buttonImage.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonImage;

        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        });

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

    private UnityEngine.UI.Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image)
        );
        imageObject.layer = gameObject.layer;
        imageObject.transform.SetParent(parent, false);

        UnityEngine.UI.Image image = imageObject.GetComponent<UnityEngine.UI.Image>();
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
