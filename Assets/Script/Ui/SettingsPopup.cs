using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 설정 팝업 UI 컨트롤러
/// - 그래픽 설정: 프레임 레이트, 밝기, 설정창 투명도
/// - 소리 설정: 마스터, BGM, 효과음, UI 볼륨
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

    // ──────── 그래픽 설정 UI ────────
    [Header("그래픽 - 프레임")]
    public TMP_Dropdown frameRateDropdown;   // 60 / 144 / 240 / 무제한

    [Header("그래픽 - 밝기")]
    public Slider brightnessSlider;          // 0 ~ 1 (0.5 = 기본)

    [Header("그래픽 - 설정창 투명도")]
    public Slider popupAlphaSlider;          // 0.3 ~ 1

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

    // ──────── PlayerPrefs 키 ────────
    private const string KEY_FRAMERATE   = "Settings_FrameRate";
    private const string KEY_BRIGHTNESS  = "Settings_Brightness";
    private const string KEY_POPUP_ALPHA = "Settings_PopupAlpha";

    // ──────── 프레임 레이트 옵션 ────────
    private readonly int[] frameRateOptions = { 60, 144, 240, -1 }; // -1 = 무제한

    public bool IsOpen => isOpen;

    // ──────────────────────────────────────
    //  초기화
    // ──────────────────────────────────────

    private void Awake()
    {
        Instance = this;

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
        // 프레임 레이트 드롭다운
        if (frameRateDropdown != null)
        {
            frameRateDropdown.ClearOptions();
            frameRateDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "60 FPS", "144 FPS", "240 FPS", "무제한"
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

        // 설정창 투명도 슬라이더
        if (popupAlphaSlider != null)
        {
            popupAlphaSlider.minValue = 0.3f;
            popupAlphaSlider.maxValue = 1f;
            popupAlphaSlider.onValueChanged.AddListener(OnPopupAlphaChanged);
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

        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.minValue = 0f;
            uiVolumeSlider.maxValue = 1f;
            uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
        }

        // 버튼
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (saveAndExitButton != null)
            saveAndExitButton.onClick.AddListener(OnSaveAndExit);
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
            canvas.sortingOrder = 999; // 가장 위에 표시

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

        settingsPanel.SetActive(true);
        StartCoroutine(FadeIn());

        // 게임씬에서는 일시정지 시도
        TryPauseGame();
    }

    public void Close()
    {
        if (!isOpen || isFading) return;

        // 설정 저장
        SaveSettings();

        StartCoroutine(FadeOut());

        // 일시정지 해제
        TryResumeGame();
    }

    private IEnumerator FadeIn()
    {
        isFading = true;

        float elapsed = 0f;
        float targetAlpha = PlayerPrefs.GetFloat(KEY_POPUP_ALPHA, 1f);

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

    private void OnPopupAlphaChanged(float value)
    {
        // 현재 열려있으면 실시간 반영
        if (isOpen && panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = value;
        }

        PlayerPrefs.SetFloat(KEY_POPUP_ALPHA, value);
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
            AudioManager.Instance.SFXVolume = value;
    }

    private void OnUIVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.UIVolume = value;
    }

    // ──────────────────────────────────────
    //  저장 / 불러오기
    // ──────────────────────────────────────

    private void RefreshUIValues()
    {
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

        // 설정창 투명도
        if (popupAlphaSlider != null)
        {
            float alpha = PlayerPrefs.GetFloat(KEY_POPUP_ALPHA, 1f);
            popupAlphaSlider.SetValueWithoutNotify(alpha);
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

            if (uiVolumeSlider != null)
                uiVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.UIVolume);
        }
    }

    private void LoadSettings()
    {
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
