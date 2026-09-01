using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 설정 팝업 UI 컨트롤러
/// - 화면 설정: 화면 모드, 해상도, 프레임 레이트, 밝기
/// - 소리 설정: 마스터, BGM, 효과음
/// - 게임 설정: 화면 흔들림 강도
/// - 페이드 인/아웃 애니메이션
/// - ESC 키로 열기/닫기
/// - 어느 씬에서든 재사용 가능
/// </summary>
public class SettingsPopup : MonoBehaviour
{
    public static SettingsPopup Instance { get; private set; }

    [Header("Panel")]
    public GameObject settingsPanel;         // 설정창 전체 패널
    public CanvasGroup panelCanvasGroup;     // 페이드 인/아웃용

    [Header("페이드 설정")]
    public float fadeDuration = 0.3f;

    [Header("옵션 UI 폰트")]
    public TMP_FontAsset optionFont;

    // ──────── 그래픽 설정 UI ────────
    [Header("그래픽 - 프레임")]
    public TMP_Dropdown frameRateDropdown;   // 60 / 144 / 240 / 무제한

    [Header("그래픽 - 밝기")]
    public Slider brightnessSlider;          // 0 ~ 1 (0.5 = 기본)

    [Header("게임 - 화면 흔들림")]
    public Slider popupAlphaSlider;          // 기존 투명도 슬라이더를 흔들림 강도로 재사용

    // ──────── 소리 설정 UI ────────
    [Header("소리 - 마스터")]
    public Slider masterVolumeSlider;

    [Header("소리 - BGM")]
    public Slider bgmVolumeSlider;

    [Header("소리 - 효과음")]
    public Slider sfxVolumeSlider;

    [Header("소리 - UI")]
    public Slider uiVolumeSlider;

    // ──────── 버튼 ────────
    [Header("버튼")]
    public Button closeButton;               // X 버튼

    // ──────── 게임씬 전용 (선택 사항) ────────
    [Header("게임씬 전용 (비어있으면 무시)")]
    public Button saveAndExitButton;         // 저장 후 메인으로

    // ──────── 내부 상태 ────────
    private bool isOpen = false;
    private bool isFading = false;
    private Image brightnessOverlay;         // 밝기 조절용 오버레이
    private TMP_Dropdown screenModeDropdown;
    private TMP_Dropdown resolutionDropdown;
    private Button defaultsButton;
    private Button saveButton;
    private Button cancelButton;
    private readonly List<Resolution> availableResolutions = new List<Resolution>();
    private RectTransform settingsRect;
    private RectTransform settingsParentRect;
    private Vector2 lastParentSize;
    private SettingsSnapshot openingSnapshot;
    private bool hasOpeningSnapshot;

    private struct SettingsSnapshot
    {
        public int screenMode;
        public int resolution;
        public int frameRate;
        public int shakeLevel;
        public float brightness;
        public float masterVolume;
        public float bgmVolume;
        public float sfxVolume;
    }

    private static readonly Vector2 SETTINGS_BASE_SIZE = new Vector2(760f, 850f);
    private const float SETTINGS_SCREEN_MARGIN = 32f;

    // ──────── PlayerPrefs 키 ────────
    private const string KEY_FRAMERATE   = "Settings_FrameRate";
    private const string KEY_BRIGHTNESS  = "Settings_Brightness";
    private const string KEY_SCREEN_MODE = "Settings_ScreenMode";
    private const string KEY_RESOLUTION  = "Settings_Resolution";
    private const string KEY_SHAKE_LEVEL = "Settings_ScreenShakeLevel";

    // ──────── 프레임 레이트 옵션 ────────
    private readonly int[] frameRateOptions = { 60, 120, 144, -1 }; // -1 = 무제한

    public bool IsOpen => isOpen;
    public static float ScreenShakeIntensity
    {
        get
        {
            return PlayerPrefs.GetInt(KEY_SHAKE_LEVEL, 2) switch
            {
                0 => 0.35f,
                1 => 0.7f,
                _ => 1f
            };
        }
    }

    // ──────────────────────────────────────
    //  초기화
    // ──────────────────────────────────────

    private void Awake()
    {
        Instance = this;

        SetupSettingsLayout();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;
    }

    private void Start()
    {
        SetupUI();
        LoadSettings();
        SetupBrightnessOverlay();
        ApplyResponsiveScale();
    }

    private void LateUpdate()
    {
        if (settingsParentRect == null) return;

        Vector2 currentSize = settingsParentRect.rect.size;
        if ((currentSize - lastParentSize).sqrMagnitude > 0.01f)
            ApplyResponsiveScale();
    }

    private void Update()
    {
        // ESC 키로 설정창 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
                Close();
            else
                Open();
        }
    }

    // ──────────────────────────────────────
    //  UI 세팅
    // ──────────────────────────────────────

    private void SetupUI()
    {
        SetupDisplayOptions();

        // 프레임 레이트 드롭다운
        if (frameRateDropdown != null)
        {
            frameRateDropdown.ClearOptions();
            frameRateDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "60 FPS", "120 FPS", "144 FPS", "무제한"
            });
            frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);
        }

        // 밝기 슬라이더
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        // 화면 흔들림 강도 슬라이더 (약함 / 보통 / 강함)
        if (popupAlphaSlider != null)
        {
            popupAlphaSlider.minValue = 0f;
            popupAlphaSlider.maxValue = 2f;
            popupAlphaSlider.wholeNumbers = true;
            popupAlphaSlider.onValueChanged.AddListener(OnScreenShakeChanged);
        }

        // 볼륨 슬라이더들
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.minValue = 0f;
            bgmVolumeSlider.maxValue = 1f;
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // 버튼
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (defaultsButton != null)
            defaultsButton.onClick.AddListener(ApplyDefaultSettings);

        if (saveButton != null)
            saveButton.onClick.AddListener(SaveAndClose);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelAndClose);

        if (saveAndExitButton != null)
            saveAndExitButton.onClick.AddListener(OnSaveAndExit);
    }

    private void SetupDisplayOptions()
    {
        BuildResolutionList();

        if (screenModeDropdown != null)
        {
            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(new List<string>
            {
                "전체 화면", "테두리 없는 창", "창 모드"
            });
            screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
        }

        if (resolutionDropdown != null)
        {
            var labels = new List<string>();
            foreach (Resolution resolution in availableResolutions)
                labels.Add(resolution.width + " × " + resolution.height);

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
    }

    private void BuildResolutionList()
    {
        availableResolutions.Clear();

        foreach (Resolution resolution in Screen.resolutions)
        {
            bool alreadyAdded = false;
            foreach (Resolution added in availableResolutions)
            {
                if (added.width == resolution.width && added.height == resolution.height)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
                availableResolutions.Add(resolution);
        }

        if (availableResolutions.Count == 0)
        {
            Resolution current = Screen.currentResolution;
            availableResolutions.Add(current);
        }
    }

    // ──────────────────────────────────────
    //  반응형 레이아웃 / 디자인
    // ──────────────────────────────────────

    private void SetupSettingsLayout()
    {
        if (settingsPanel == null) return;

        settingsRect = settingsPanel.GetComponent<RectTransform>();
        settingsParentRect = settingsRect != null ? settingsRect.parent as RectTransform : null;
        if (settingsRect == null) return;

        settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
        settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
        settingsRect.pivot = new Vector2(0.5f, 0.5f);
        settingsRect.anchoredPosition = Vector2.zero;
        settingsRect.sizeDelta = SETTINGS_BASE_SIZE;

        Image panelImage = settingsPanel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.color = new Color(0.055f, 0.075f, 0.12f, 0.97f);

        screenModeDropdown = CreateDropdown("ScreenModeDropdown");
        resolutionDropdown = CreateDropdown("ResolutionDropdown");
        CreateLabel("ScreenMode", "화면 모드");
        CreateLabel("Resolution", "해상도");
        CreateLabel("ShakeValue", "강함");

        RenameLabel("Popup", "화면 흔들림");
        SetElementActive("UI", false);
        if (uiVolumeSlider != null)
            uiVolumeSlider.gameObject.SetActive(false);

        SetupText("TitleText", new Vector2(0f, 367f), new Vector2(430f, 64f), 42f, true);
        SetupText("ScreenMode", new Vector2(-205f, 300f), new Vector2(210f, 48f), 25f);
        SetupText("Resolution", new Vector2(-205f, 235f), new Vector2(210f, 48f), 25f);
        SetupText("Frame", new Vector2(-205f, 170f), new Vector2(210f, 48f), 25f);
        SetupText("Brightness", new Vector2(-205f, 105f), new Vector2(210f, 48f), 25f);
        SetupText("Popup", new Vector2(-205f, 35f), new Vector2(210f, 48f), 25f);
        SetupText("ShakeValue", new Vector2(300f, 35f), new Vector2(90f, 48f), 22f);
        SetupText("Master", new Vector2(-205f, -70f), new Vector2(210f, 48f), 25f);
        SetupText("BGM", new Vector2(-205f, -145f), new Vector2(210f, 48f), 25f);
        SetupText("SFX", new Vector2(-205f, -220f), new Vector2(210f, 48f), 25f);

        SetupControl(screenModeDropdown, new Vector2(145f, 300f), new Vector2(390f, 50f));
        SetupControl(resolutionDropdown, new Vector2(145f, 235f), new Vector2(390f, 50f));
        SetupControl(frameRateDropdown, new Vector2(145f, 170f), new Vector2(390f, 50f));
        SetupControl(brightnessSlider, new Vector2(145f, 105f), new Vector2(390f, 38f));
        SetupControl(popupAlphaSlider, new Vector2(95f, 35f), new Vector2(280f, 38f));
        SetupControl(masterVolumeSlider, new Vector2(145f, -70f), new Vector2(390f, 38f));
        SetupControl(bgmVolumeSlider, new Vector2(145f, -145f), new Vector2(390f, 38f));
        SetupControl(sfxVolumeSlider, new Vector2(145f, -220f), new Vector2(390f, 38f));

        defaultsButton = CreateActionButton(
            "DefaultsButton", "기본값", new Vector2(-210f, -345f),
            new Color(0.26f, 0.30f, 0.38f, 1f));
        saveButton = CreateActionButton(
            "SaveButton", "저장", new Vector2(0f, -345f),
            new Color(0.63f, 0.43f, 0.16f, 1f));
        cancelButton = CreateActionButton(
            "CancelButton", "취소", new Vector2(210f, -345f),
            new Color(0.40f, 0.22f, 0.24f, 1f));

        if (closeButton != null)
        {
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = Vector2.one;
            closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-22f, -22f);
            closeRect.sizeDelta = new Vector2(48f, 48f);

            Image closeImage = closeButton.GetComponent<Image>();
            if (closeImage != null)
                closeImage.color = new Color(0.72f, 0.20f, 0.18f, 1f);

            SetupCloseButtonLabel();
        }

        ApplyOptionFonts();

        ApplyResponsiveScale();
    }

    private void SetupCloseButtonLabel()
    {
        if (closeButton == null) return;

        TMP_Text closeText = closeButton.GetComponentInChildren<TMP_Text>(true);
        if (closeText == null)
        {
            GameObject textObject = new GameObject(
                "CloseLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(closeButton.transform, false);
            closeText = textObject.GetComponent<TMP_Text>();
        }

        RectTransform textRect = closeText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        closeText.text = "X";
        closeText.fontSize = 28f;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.raycastTarget = false;
        ApplyFont(closeText, Color.white);
        closeText.fontStyle = FontStyles.Bold;
    }

    private Button CreateActionButton(string objectName, string label, Vector2 position, Color color)
    {
        Transform existing = settingsPanel.transform.Find(objectName);
        if (existing != null)
            return existing.GetComponent<Button>();

        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(settingsPanel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(170f, 56f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
        colors.pressedColor = new Color(0.78f, 0.68f, 0.48f, 1f);
        button.colors = colors;

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = label;
        text.fontSize = 25f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        ApplyFont(text, Color.white);

        return button;
    }

    private void ApplyOptionFonts()
    {
        if (optionFont == null || settingsPanel == null) return;

        string[] labelNames =
        {
            "TitleText", "ScreenMode", "Resolution", "Frame", "Brightness",
            "Popup", "ShakeValue", "Master", "BGM", "SFX"
        };

        foreach (string labelName in labelNames)
        {
            Transform child = settingsPanel.transform.Find(labelName);
            if (child == null) continue;

            TMP_Text text = child.GetComponent<TMP_Text>();
            ApplyFont(text, text != null ? text.color : Color.white);
        }

        ApplyDropdownFont(screenModeDropdown);
        ApplyDropdownFont(resolutionDropdown);
        ApplyDropdownFont(frameRateDropdown);

        Transform title = settingsPanel.transform.Find("TitleText");
        if (title != null && title.TryGetComponent(out TMP_Text titleText))
            titleText.fontStyle = FontStyles.Bold;
    }

    private void ApplyDropdownFont(TMP_Dropdown dropdown)
    {
        if (dropdown == null || optionFont == null) return;

        if (dropdown.captionText != null)
            ApplyFont(dropdown.captionText, new Color(0.12f, 0.14f, 0.18f, 1f));

        if (dropdown.itemText != null)
            ApplyFont(dropdown.itemText, new Color(0.12f, 0.14f, 0.18f, 1f));

        foreach (TMP_Text text in dropdown.GetComponentsInChildren<TMP_Text>(true))
            ApplyFont(text, new Color(0.12f, 0.14f, 0.18f, 1f));
    }

    private void ApplyFont(TMP_Text text, Color color)
    {
        if (text == null || optionFont == null) return;

        text.font = optionFont;
        text.fontSharedMaterial = optionFont.material;
        text.color = color;
        text.fontStyle = FontStyles.Normal;
    }

    private TMP_Dropdown CreateDropdown(string objectName)
    {
        if (frameRateDropdown == null) return null;

        Transform existing = settingsPanel.transform.Find(objectName);
        if (existing != null)
            return existing.GetComponent<TMP_Dropdown>();

        GameObject clone = Instantiate(frameRateDropdown.gameObject, settingsPanel.transform);
        clone.name = objectName;
        return clone.GetComponent<TMP_Dropdown>();
    }

    private void CreateLabel(string objectName, string textValue)
    {
        if (settingsPanel.transform.Find(objectName) != null) return;

        Transform source = settingsPanel.transform.Find("Frame");
        if (source == null) return;

        GameObject clone = Instantiate(source.gameObject, settingsPanel.transform);
        clone.name = objectName;

        TMP_Text text = clone.GetComponent<TMP_Text>();
        if (text != null)
            text.text = textValue;
    }

    private void RenameLabel(string objectName, string textValue)
    {
        Transform child = settingsPanel.transform.Find(objectName);
        if (child == null) return;

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text != null)
            text.text = textValue;
    }

    private void SetElementActive(string objectName, bool active)
    {
        Transform child = settingsPanel.transform.Find(objectName);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private void SetupText(string objectName, Vector2 position, Vector2 size, float fontSize, bool isTitle = false)
    {
        Transform child = settingsPanel.transform.Find(objectName);
        if (child == null) return;

        RectTransform rect = child as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text == null) return;

        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = isTitle
            ? new Color(1f, 0.82f, 0.42f, 1f)
            : new Color(0.93f, 0.94f, 0.98f, 1f);
        if (isTitle)
            text.fontStyle = FontStyles.Bold;
    }

    private void SetupControl(Selectable control, Vector2 position, Vector2 size)
    {
        if (control == null) return;

        RectTransform rect = control.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        ColorBlock colors = control.colors;
        colors.normalColor = new Color(0.82f, 0.84f, 0.88f, 1f);
        colors.highlightedColor = new Color(1f, 0.86f, 0.55f, 1f);
        colors.pressedColor = new Color(0.86f, 0.64f, 0.28f, 1f);
        colors.selectedColor = colors.highlightedColor;
        control.colors = colors;
    }

    private void ApplyResponsiveScale()
    {
        if (settingsRect == null || settingsParentRect == null) return;

        Vector2 available = settingsParentRect.rect.size - Vector2.one * SETTINGS_SCREEN_MARGIN * 2f;
        float scale = Mathf.Min(1f, available.x / SETTINGS_BASE_SIZE.x, available.y / SETTINGS_BASE_SIZE.y);
        scale = Mathf.Max(0.45f, scale);

        settingsRect.localScale = Vector3.one * scale;
        settingsRect.anchoredPosition = Vector2.zero;
        lastParentSize = settingsParentRect.rect.size;
    }

    // ──────────────────────────────────────
    //  밝기 오버레이
    // ──────────────────────────────────────

    /// <summary>
    /// 밝기 조절용 검정 오버레이를 자동 생성
    /// 별도의 Canvas에 만들어서 모든 UI 위에 렌더링되지 않도록 조절
    /// </summary>
    private void SetupBrightnessOverlay()
    {
        // "BrightnessOverlay"라는 이름의 Canvas를 찾거나 새로 만듦
        GameObject overlayObj = GameObject.Find("BrightnessOverlayCanvas");

        if (overlayObj == null)
        {
            overlayObj = new GameObject("BrightnessOverlayCanvas");
            Canvas canvas = overlayObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // 게임 화면보다 위, 설정 UI보다 아래

            overlayObj.AddComponent<CanvasScaler>();

            // Raycast를 막지 않도록 GraphicRaycaster는 추가하지 않음

            GameObject imageObj = new GameObject("BrightnessImage");
            imageObj.transform.SetParent(overlayObj.transform, false);

            brightnessOverlay = imageObj.AddComponent<Image>();
            brightnessOverlay.color = new Color(0, 0, 0, 0);
            brightnessOverlay.raycastTarget = false; // 클릭 안 막음

            // 화면 전체 채우기
            RectTransform rt = imageObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            DontDestroyOnLoad(overlayObj);
        }
        else
        {
            brightnessOverlay = overlayObj.GetComponentInChildren<Image>();
        }

        // 저장된 밝기 적용
        float brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 0.5f);
        ApplyBrightness(brightness);
    }

    // ──────────────────────────────────────
    //  열기 / 닫기
    // ──────────────────────────────────────

    public void Open()
    {
        if (isOpen || isFading) return;

        // UI 값 갱신
        RefreshUIValues();
        openingSnapshot = CaptureCurrentSettings();
        hasOpeningSnapshot = true;

        settingsPanel.SetActive(true);
        StartCoroutine(FadeIn());

        // 게임씬에서는 일시정지 시도
        TryPauseGame();
    }

    public void Close()
    {
        CancelAndClose();
    }

    private void SaveAndClose()
    {
        if (!isOpen || isFading) return;

        SaveSettings();
        hasOpeningSnapshot = false;
        BeginClose();
    }

    private void CancelAndClose()
    {
        if (!isOpen || isFading) return;

        if (hasOpeningSnapshot)
            ApplySnapshot(openingSnapshot);

        hasOpeningSnapshot = false;
        BeginClose();
    }

    private void BeginClose()
    {
        if (!isOpen || isFading) return;

        StartCoroutine(FadeOut());

        TryResumeGame();
    }

    private IEnumerator FadeIn()
    {
        isFading = true;

        float elapsed = 0f;
        const float targetAlpha = 1f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 일시정지 중에도 작동
            float t = elapsed / fadeDuration;
            panelCanvasGroup.alpha = Mathf.Lerp(0f, targetAlpha, t);
            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        isOpen = true;
        isFading = false;
    }

    private IEnumerator FadeOut()
    {
        isFading = true;

        float elapsed = 0f;
        float startAlpha = panelCanvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        panelCanvasGroup.alpha = 0f;
        settingsPanel.SetActive(false);
        isOpen = false;
        isFading = false;
    }

    // ──────────────────────────────────────
    //  일시정지 (게임씬 전용)
    // ──────────────────────────────────────

    private float savedTimeScale = 1f;

    private void TryPauseGame()
    {
        // GameManager가 있으면 게임씬으로 판단
        if (GameManager.Instance != null)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Debug.Log("게임 일시정지");
        }
    }

    private void TryResumeGame()
    {
        if (GameManager.Instance != null)
        {
            Time.timeScale = savedTimeScale;
            Debug.Log("게임 재개");
        }
    }

    // ──────────────────────────────────────
    //  콜백: 그래픽 설정
    // ──────────────────────────────────────

    private void OnFrameRateChanged(int index)
    {
        int fps = frameRateOptions[index];

        if (fps == -1)
        {
            Application.targetFrameRate = -1; // 무제한
            QualitySettings.vSyncCount = 0;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
        }

        PlayerPrefs.SetInt(KEY_FRAMERATE, index);
        Debug.Log("프레임 레이트 변경: " + (fps == -1 ? "무제한" : fps + " FPS"));
    }

    private void OnScreenModeChanged(int index)
    {
        PlayerPrefs.SetInt(KEY_SCREEN_MODE, index);
        ApplyDisplaySettings();
    }

    private void OnResolutionChanged(int index)
    {
        PlayerPrefs.SetInt(KEY_RESOLUTION, index);
        ApplyDisplaySettings();
    }

    private void ApplyDisplaySettings()
    {
        if (availableResolutions.Count == 0) return;

        int resolutionIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(KEY_RESOLUTION, FindCurrentResolutionIndex()),
            0,
            availableResolutions.Count - 1);
        int modeIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(KEY_SCREEN_MODE, FindCurrentScreenModeIndex()),
            0,
            2);

        FullScreenMode mode = modeIndex switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.Windowed
        };

        Resolution resolution = availableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, mode);
    }

    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            Resolution resolution = availableResolutions[i];
            if (resolution.width == Screen.width && resolution.height == Screen.height)
                return i;
        }

        return Mathf.Max(0, availableResolutions.Count - 1);
    }

    private int FindCurrentScreenModeIndex()
    {
        return Screen.fullScreenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => 0,
            FullScreenMode.FullScreenWindow => 1,
            _ => 2
        };
    }

    private void OnBrightnessChanged(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat(KEY_BRIGHTNESS, value);
    }

    private void ApplyBrightness(float value)
    {
        // value: 0 = 완전 어두움, 0.5 = 기본, 1 = 밝음 (어두운 오버레이 없음)
        // 오버레이의 알파를 (1 - value) * 0.8 로 설정 (최대 80% 어두워짐)
        if (brightnessOverlay != null)
        {
            float alpha = (1f - value) * 0.8f;
            brightnessOverlay.color = new Color(0, 0, 0, alpha);
        }
    }

    private void OnScreenShakeChanged(float value)
    {
        int level = Mathf.Clamp(Mathf.RoundToInt(value), 0, 2);
        PlayerPrefs.SetInt(KEY_SHAKE_LEVEL, level);
        UpdateShakeValueLabel(level);
    }

    private void UpdateShakeValueLabel(int level)
    {
        Transform child = settingsPanel != null ? settingsPanel.transform.Find("ShakeValue") : null;
        if (child == null) return;

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text == null) return;

        text.text = level switch
        {
            0 => "약함",
            1 => "보통",
            _ => "강함"
        };
    }

    // ──────────────────────────────────────
    //  콜백: 소리 설정
    // ──────────────────────────────────────

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.MasterVolume = value;
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.BGMVolume = value;
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SFXVolume = value;
            AudioManager.Instance.UIVolume = value;
        }
    }

    // ──────────────────────────────────────
    //  저장 / 불러오기 / 기본값 복원
    // ──────────────────────────────────────

    private SettingsSnapshot CaptureCurrentSettings()
    {
        return new SettingsSnapshot
        {
            screenMode = screenModeDropdown != null
                ? screenModeDropdown.value
                : PlayerPrefs.GetInt(KEY_SCREEN_MODE, FindCurrentScreenModeIndex()),
            resolution = resolutionDropdown != null
                ? resolutionDropdown.value
                : PlayerPrefs.GetInt(KEY_RESOLUTION, FindCurrentResolutionIndex()),
            frameRate = frameRateDropdown != null
                ? frameRateDropdown.value
                : PlayerPrefs.GetInt(KEY_FRAMERATE, 0),
            shakeLevel = popupAlphaSlider != null
                ? Mathf.RoundToInt(popupAlphaSlider.value)
                : PlayerPrefs.GetInt(KEY_SHAKE_LEVEL, 2),
            brightness = brightnessSlider != null
                ? brightnessSlider.value
                : PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 0.5f),
            masterVolume = masterVolumeSlider != null ? masterVolumeSlider.value : 1f,
            bgmVolume = bgmVolumeSlider != null ? bgmVolumeSlider.value : 1f,
            sfxVolume = sfxVolumeSlider != null ? sfxVolumeSlider.value : 1f
        };
    }

    private void ApplyDefaultSettings()
    {
        SettingsSnapshot defaults = new SettingsSnapshot
        {
            screenMode = 2,
            resolution = FindClosestResolutionIndex(1920, 1080),
            frameRate = 0,
            shakeLevel = 1,
            brightness = 1f,
            masterVolume = 0.5f,
            bgmVolume = 0.5f,
            sfxVolume = 0.5f
        };

        ApplySnapshot(defaults);
    }

    private void ApplySnapshot(SettingsSnapshot settings)
    {
        int screenMode = Mathf.Clamp(settings.screenMode, 0, 2);
        int resolution = availableResolutions.Count > 0
            ? Mathf.Clamp(settings.resolution, 0, availableResolutions.Count - 1)
            : 0;
        int frameRate = Mathf.Clamp(settings.frameRate, 0, frameRateOptions.Length - 1);
        int shakeLevel = Mathf.Clamp(settings.shakeLevel, 0, 2);

        if (screenModeDropdown != null)
            screenModeDropdown.SetValueWithoutNotify(screenMode);
        if (resolutionDropdown != null)
            resolutionDropdown.SetValueWithoutNotify(resolution);
        if (frameRateDropdown != null)
            frameRateDropdown.SetValueWithoutNotify(frameRate);
        if (brightnessSlider != null)
            brightnessSlider.SetValueWithoutNotify(settings.brightness);
        if (popupAlphaSlider != null)
            popupAlphaSlider.SetValueWithoutNotify(shakeLevel);
        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(settings.bgmVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);

        PlayerPrefs.SetInt(KEY_SCREEN_MODE, screenMode);
        PlayerPrefs.SetInt(KEY_RESOLUTION, resolution);
        ApplyDisplaySettings();
        OnFrameRateChanged(frameRate);
        OnBrightnessChanged(settings.brightness);
        OnScreenShakeChanged(shakeLevel);
        OnMasterVolumeChanged(settings.masterVolume);
        OnBGMVolumeChanged(settings.bgmVolume);
        OnSFXVolumeChanged(settings.sfxVolume);
    }

    private int FindClosestResolutionIndex(int targetWidth, int targetHeight)
    {
        if (availableResolutions.Count == 0) return 0;

        int closestIndex = 0;
        long closestDistance = long.MaxValue;

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            Resolution resolution = availableResolutions[i];
            long widthDifference = resolution.width - targetWidth;
            long heightDifference = resolution.height - targetHeight;
            long distance = widthDifference * widthDifference + heightDifference * heightDifference;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void RefreshUIValues()
    {
        if (screenModeDropdown != null)
            screenModeDropdown.SetValueWithoutNotify(
                Mathf.Clamp(PlayerPrefs.GetInt(KEY_SCREEN_MODE, FindCurrentScreenModeIndex()), 0, 2));

        if (resolutionDropdown != null && availableResolutions.Count > 0)
        {
            int resolutionIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(KEY_RESOLUTION, FindCurrentResolutionIndex()),
                0,
                availableResolutions.Count - 1);
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
        }

        // 프레임 레이트
        if (frameRateDropdown != null)
        {
            int savedIndex = PlayerPrefs.GetInt(KEY_FRAMERATE, 0);
            frameRateDropdown.SetValueWithoutNotify(savedIndex);
        }

        // 밝기
        if (brightnessSlider != null)
        {
            float brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 0.5f);
            brightnessSlider.SetValueWithoutNotify(brightness);
        }

        // 화면 흔들림
        if (popupAlphaSlider != null)
        {
            int shakeLevel = Mathf.Clamp(PlayerPrefs.GetInt(KEY_SHAKE_LEVEL, 2), 0, 2);
            popupAlphaSlider.SetValueWithoutNotify(shakeLevel);
            UpdateShakeValueLabel(shakeLevel);
        }

        // 볼륨
        if (AudioManager.Instance != null)
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);

            if (bgmVolumeSlider != null)
                bgmVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);

        }
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(KEY_RESOLUTION) || PlayerPrefs.HasKey(KEY_SCREEN_MODE))
            ApplyDisplaySettings();

        // 프레임 레이트 적용
        int frameIndex = PlayerPrefs.GetInt(KEY_FRAMERATE, 0);
        if (frameIndex >= 0 && frameIndex < frameRateOptions.Length)
        {
            int fps = frameRateOptions[frameIndex];
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
        }

        // 밝기 적용
        float brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 0.5f);
        ApplyBrightness(brightness);

        if (AudioManager.Instance != null)
            AudioManager.Instance.UIVolume = AudioManager.Instance.SFXVolume;
    }

    private void SaveSettings()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SaveAllSettings();

        PlayerPrefs.Save();
        Debug.Log("설정 저장 완료");
    }

    // ──────────────────────────────────────
    //  저장 후 메인으로 (게임씬 전용)
    // ──────────────────────────────────────

    private void OnSaveAndExit()
    {
        SaveSettings();

        // 현재 웨이브 번호 저장 (이어하기용)
        if (FindFirstObjectByType<WaveManager>() != null)
        {
            WaveManager wm = FindFirstObjectByType<WaveManager>();
            PlayerPrefs.SetInt("SavedWave", wm.CurrentWave);
            PlayerPrefs.Save();
            Debug.Log("웨이브 저장: " + wm.CurrentWave);
        }

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }
}
