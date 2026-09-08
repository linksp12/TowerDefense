using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메인 화면 매니저 (업데이트 버전)
/// - 시작, 저장된 스테이지 이어하기, 설정, 종료
/// - ESC로 설정창 열기 (SettingsPopup에서 처리)
/// - 씬 진입 시 메인 BGM 자동 재생
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    private const int RequiredTitleClicks = 10;
    private const int EasterEggPlayerHp = 50;

    private int titleClickCount;
    private Canvas mainMenuCanvas;
    private GameObject easterEggMessageObject;
    private Coroutine easterEggMessageCoroutine;

    [Header("Easter Egg")]
    [SerializeField] private AudioClip easterEggSuccessSound;
    [SerializeField] private TMP_FontAsset easterEggFont;

    public static int EasterEggPlayerHpOverride { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEasterEggBonus()
    {
        EasterEggPlayerHpOverride = 0;
    }

    private void Start()
    {
        // 메인 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGMForScene("MainScene");
        }

        SetupTitleEasterEgg();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && IsPointerOverTitleT())
            OnTitleTClicked();
    }

    private void SetupTitleEasterEgg()
    {
        GameObject startButton = GameObject.Find("StartButton");
        mainMenuCanvas = startButton != null
            ? startButton.GetComponentInParent<Canvas>()
            : null;

        if (mainMenuCanvas == null)
        {
            Debug.LogWarning("MainMenuManager: 이스터에그 버튼을 생성할 Canvas가 없습니다.");
        }
    }

    private bool IsPointerOverTitleT()
    {
        if (mainMenuCanvas == null ||
            (SettingsPopup.Instance != null && SettingsPopup.Instance.IsOpen))
        {
            return false;
        }

        Vector2 pointer = Input.mousePosition;
        float normalizedX = pointer.x / Mathf.Max(1f, Screen.width);
        float normalizedY = pointer.y / Mathf.Max(1f, Screen.height);

        return normalizedX >= 0.535f && normalizedX <= 0.615f &&
               normalizedY >= 0.80f && normalizedY <= 0.94f;
    }

    private void OnTitleTClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        titleClickCount++;

        if (titleClickCount < RequiredTitleClicks)
            return;

        titleClickCount = 0;

        bool isEnabled = EasterEggPlayerHpOverride <= 0;
        EasterEggPlayerHpOverride = isEnabled ? EasterEggPlayerHp : 0;

        if (AudioManager.Instance != null && easterEggSuccessSound != null)
        {
            AudioManager.Instance.PlayUISound(easterEggSuccessSound);
        }
        else if (easterEggSuccessSound == null)
        {
            Debug.LogWarning(
                "MainMenuManager: Easter Egg Success Sound가 연결되지 않았습니다."
            );
        }

        ShowEasterEggMessage(isEnabled);
        Debug.Log(
            isEnabled
                ? "이스터에그 활성화: 이번 플레이의 기지 체력이 50으로 변경됨"
                : "이스터에그 해제: 이번 플레이의 기지 체력이 15로 복구됨"
        );
    }

    private void ShowEasterEggMessage(bool isEnabled)
    {
        string message = isEnabled
            ? "숨겨진 힘이 깨어났습니다!  기지 체력 50"
            : "숨겨진 힘이 사라졌습니다.  기지 체력 15";

        ShowMainMenuMessage(message, new Color32(255, 205, 75, 255));
    }

    private void ShowMainMenuMessage(string message, Color textColor)
    {
        if (mainMenuCanvas == null)
            return;

        if (easterEggMessageCoroutine != null)
        {
            StopCoroutine(easterEggMessageCoroutine);
            easterEggMessageCoroutine = null;
        }

        if (easterEggMessageObject != null)
            Destroy(easterEggMessageObject);

        GameObject messageObject = new GameObject(
            "EasterEggMessage",
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup)
        );

        easterEggMessageObject = messageObject;

        messageObject.layer = mainMenuCanvas.gameObject.layer;
        messageObject.transform.SetParent(mainMenuCanvas.transform, false);
        messageObject.transform.SetAsLastSibling();

        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.58f);
        messageRect.anchorMax = new Vector2(0.5f, 0.58f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = Vector2.zero;
        messageRect.sizeDelta = new Vector2(620f, 76f);

        Image messageBackground = messageObject.GetComponent<Image>();
        messageBackground.color = new Color(0.02f, 0.025f, 0.04f, 0.9f);
        messageBackground.raycastTarget = false;

        GameObject textObject = new GameObject(
            "MessageText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.layer = mainMenuCanvas.gameObject.layer;
        textObject.transform.SetParent(messageObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(584f, 60f);

        TextMeshProUGUI messageText = textObject.GetComponent<TextMeshProUGUI>();
        messageText.text = message;
        messageText.font = easterEggFont != null
            ? easterEggFont
            : FindMaplestoryFont();
        messageText.fontSize = 28f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.color = textColor;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.overflowMode = TextOverflowModes.Overflow;
        messageText.raycastTarget = false;
        messageText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        messageText.outlineWidth = 0.15f;

        CanvasGroup canvasGroup = messageObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        easterEggMessageCoroutine = StartCoroutine(
            ShowEasterEggMessageRoutine(messageObject, canvasGroup)
        );
    }

    private IEnumerator ShowEasterEggMessageRoutine(
        GameObject messageObject,
        CanvasGroup canvasGroup)
    {
        float elapsed = 0f;
        const float fadeDuration = 0.25f;

        canvasGroup.alpha = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.5f);

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        Destroy(messageObject);

        if (easterEggMessageObject == messageObject)
            easterEggMessageObject = null;

        easterEggMessageCoroutine = null;
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
            "MainMenuManager: Maplestory Bold SDF 폰트를 찾지 못했습니다."
        );
        return TMP_Settings.defaultFontAsset;
    }

    // ──────── 시작 버튼 ────────
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("StoryScene");
    }

    // ──────── 이어하기 버튼 ────────
    public void OnContinueButtonClicked()
    {
        if (!GameUIManager.TryGetSavedStageScene(out string savedStageScene))
        {
            ShowMainMenuMessage(
                "저장 데이터가 없습니다.",
                new Color32(255, 205, 75, 255)
            );
            Debug.Log("이어하기 - 저장 데이터 없음");
            return;
        }

        Debug.Log($"이어하기 - {savedStageScene}으로 이동");
        SceneManager.LoadScene(savedStageScene);
    }

    // ──────── 설정 버튼 ────────
    public void OnSettingsButtonClicked()
    {
        if (SettingsPopup.Instance != null)
        {
            if (SettingsPopup.Instance.IsOpen)
                SettingsPopup.Instance.Close();
            else
                SettingsPopup.Instance.Open();
        }
        else
        {
            Debug.LogWarning("SettingsPopup이 씬에 없습니다.");
        }
    }

    // ──────── 종료 버튼 ────────
    public void OnQuitButtonClicked()
    {
        Debug.Log("게임 종료");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
