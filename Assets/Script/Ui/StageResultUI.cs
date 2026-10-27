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
        bool skinned = BattleUiSkin.Skin(panel, BattleUiSkin.Frame, 1f);

        // 픽셀 스킨이 있으면 제목을 패널 위 리본에 올리고, 없으면 기존 색 띠를 쓴다.
        UnityEngine.UI.Image ribbon = skinned ? CreateImage("TitleRibbon", panel.transform, Color.white) : null;
        if (ribbon != null && BattleUiSkin.Skin(ribbon, BattleUiSkin.Banner, 1f))
        {
            ribbon.color = victory ? Color.white : new Color(0.55f, 0.6f, 0.72f, 1f);
            RectTransform ribbonRect = ribbon.rectTransform;
            ribbonRect.anchorMin = ribbonRect.anchorMax = new Vector2(0.5f, 1f);
            ribbonRect.pivot = new Vector2(0.5f, 0.5f);
            ribbonRect.anchoredPosition = new Vector2(0f, -6f);
            ribbonRect.sizeDelta = new Vector2(720f, 150f);
        }
        else
        {
            ribbon = null;
        }

        // 승리 시 남은 기지 체력 비율로 점수(%)와 별 1~3개를 보여 준다.
        bool showScore = victory && ribbon != null &&
            BattleUiSkin.StarFull != null && BattleUiSkin.StarEmpty != null;
        if (showScore)
            panelRect.sizeDelta = new Vector2(900f, 560f);
        float titleY = ribbon != null ? panelRect.sizeDelta.y * 0.5f : 145f;
        float messageY = showScore ? 20f : ribbon != null ? 92f : 70f;
        float healthY = showScore ? -16f : ribbon != null ? 34f : 10f;
        float noticeY = showScore ? -54f : ribbon != null ? -30f : -55f;
        float buttonY = showScore ? -172f : -150f;

        UnityEngine.UI.Image topLine = ribbon != null ? null : CreateImage(
            "TopLine",
            panel.transform,
            victory
                ? new Color(1f, 0.73f, 0.18f, 1f)
                : new Color(0.78f, 0.2f, 0.18f, 1f)
        );
        if (topLine != null)
        {
            RectTransform topLineRect = topLine.rectTransform;
            topLineRect.anchorMin = new Vector2(0f, 1f);
            topLineRect.anchorMax = new Vector2(1f, 1f);
            topLineRect.pivot = new Vector2(0.5f, 1f);
            topLineRect.anchoredPosition = Vector2.zero;
            topLineRect.sizeDelta = new Vector2(0f, 10f);
        }

        CreateText(
            "ResultTitle",
            panel.transform,
            victory ? GetStageLabel(currentSceneName) + " 클리어!" : "전투 실패",
            new Vector2(0f, titleY),
            new Vector2(600f, 80f),
            ribbon != null ? 48f : 54f,
            ribbon != null
                ? (victory ? new Color(1f, 0.93f, 0.67f, 1f) : new Color(0.95f, 0.92f, 0.86f, 1f))
                : (victory ? new Color(1f, 0.78f, 0.25f, 1f) : new Color(1f, 0.35f, 0.3f, 1f))
        );

        CreateText(
            "ResultMessage",
            panel.transform,
            victory
                ? "모든 웨이브를 막아냈습니다."
                : "왕국이 무너졌습니다. 다시 도전해 보세요.",
            new Vector2(0f, messageY),
            new Vector2(760f, 50f),
            showScore ? 26f : 29f,
            new Color(0.9f, 0.92f, 0.96f, 1f)
        );

        CreateText(
            "RemainingBaseHealth",
            panel.transform,
            $"남은 기지 체력  {remainingBaseHealth} / {maxBaseHealth}",
            new Vector2(0f, healthY),
            new Vector2(760f, 45f),
            showScore ? 25f : 28f,
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
                new Vector2(0f, noticeY),
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
                new Vector2(-270f, buttonY),
                new Vector2(230f, 74f),
                () => LoadScene(currentSceneName)
            );
            CreateButton(
                panel.transform,
                "스테이지 선택",
                new Vector2(0f, buttonY),
                new Vector2(230f, 74f),
                () => LoadScene("StageSelectionScene")
            );
            CreateButton(
                panel.transform,
                "광장으로",
                new Vector2(270f, buttonY),
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
                new Vector2(-135f, buttonY),
                new Vector2(230f, 74f),
                () => LoadScene(currentSceneName)
            );
            CreateButton(
                panel.transform,
                "광장으로",
                new Vector2(135f, buttonY),
                new Vector2(230f, 74f),
                () => LoadScene("PlazaScene"),
                true
            );
        }

        if (showScore)
            BuildScore(panel.transform, remainingBaseHealth, maxBaseHealth);

        panelRect.localScale = Vector3.one * 0.92f;
        StartCoroutine(AnimateIn());
        if (showScore)
            StartCoroutine(AnimateScore());
    }

    // ──────── 별·점수 ────────

    private const int ThreeStarPercent = 90;
    private const int TwoStarPercent = 50;
    private readonly RectTransform[] starFills = new RectTransform[3];
    private TextMeshProUGUI scoreText;
    private int scorePercent;
    private int earnedStars;

    // 점수 = 남은 기지 체력 / 최대 체력. 90% 이상 별 3개, 50% 이상 별 2개, 그 밖의 클리어는 별 1개.
    private void BuildScore(Transform panel, int remainingBaseHealth, int maxBaseHealth)
    {
        scorePercent = maxBaseHealth > 0
            ? Mathf.Clamp(Mathf.RoundToInt(100f * remainingBaseHealth / maxBaseHealth), 0, 100)
            : 0;
        earnedStars = scorePercent >= ThreeStarPercent ? 3 : scorePercent >= TwoStarPercent ? 2 : 1;

        Vector2[] positions = { new Vector2(-118f, 136f), new Vector2(0f, 146f), new Vector2(118f, 136f) };
        float[] sizes = { 84f, 104f, 84f };
        for (int i = 0; i < 3; i++)
        {
            UnityEngine.UI.Image slot = CreateImage("StarSlot" + (i + 1), panel, Color.white);
            slot.sprite = BattleUiSkin.StarEmpty;
            RectTransform slotRect = slot.rectTransform;
            slotRect.anchorMin = slotRect.anchorMax = slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = positions[i];
            slotRect.sizeDelta = new Vector2(sizes[i], sizes[i]);
            slotRect.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 8f : i == 2 ? -8f : 0f);

            UnityEngine.UI.Image fill = CreateImage("Star", slot.transform, Color.white);
            fill.sprite = BattleUiSkin.StarFull;
            Stretch(fill.rectTransform);
            starFills[i] = fill.rectTransform;
            fill.gameObject.SetActive(false);
        }

        scoreText = CreateText("Score", panel, "점수  0%", new Vector2(0f, 62f), new Vector2(400f, 50f), 34f,
            new Color(1f, 0.84f, 0.42f, 1f));
    }

    // 패널이 뜬 뒤 별이 하나씩 튀어나오고 점수가 0%부터 올라간다.
    private System.Collections.IEnumerator AnimateScore()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        const float countDuration = 0.7f;
        float elapsed = 0f;
        int nextStar = 0;
        float nextStarTime = 0f;
        float[] popStart = { -1f, -1f, -1f };

        while (elapsed < countDuration + 0.25f * earnedStars + 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            int shown = Mathf.RoundToInt(scorePercent * Mathf.Clamp01(elapsed / countDuration));
            scoreText.text = $"점수  {shown}%";

            if (nextStar < earnedStars && elapsed >= nextStarTime)
            {
                starFills[nextStar].gameObject.SetActive(true);
                popStart[nextStar] = elapsed;
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayButtonClick();
                nextStar++;
                nextStarTime = elapsed + 0.25f;
            }

            for (int i = 0; i < 3; i++)
            {
                if (popStart[i] < 0f)
                    continue;
                float p = Mathf.Clamp01((elapsed - popStart[i]) / 0.28f);
                float back = 1f + 2.7f * Mathf.Pow(p - 1f, 3f) + 1.7f * Mathf.Pow(p - 1f, 2f);
                starFills[i].localScale = Vector3.one * Mathf.Max(0f, back);
            }
            yield return null;
        }

        scoreText.text = $"점수  {scorePercent}%";
        for (int i = 0; i < earnedStars; i++)
            starFills[i].localScale = Vector3.one;
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
        bool skinned = BattleUiSkin.Skin(buttonImage,
            highlighted ? BattleUiSkin.GoldButton : BattleUiSkin.DarkButton, BattleUiSkin.PixelScale);

        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = skinned ? new Color(0.93f, 0.93f, 0.93f, 1f) : Color.white;
        colors.highlightedColor = skinned ? Color.white : new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = skinned ? colors.normalColor : colors.highlightedColor;
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
            skinned && highlighted ? new Color32(58, 31, 11, 255) : Color.white
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
