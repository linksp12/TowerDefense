using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    private const string SavedStageKey = "SavedStageScene";
    private const string SaveIconPath = "UI/SaveIcon";

    public PauseManager pauseManager;

    private TextMeshProUGUI saveMessageText;
    private Coroutine saveMessageCoroutine;

    private void Start()
    {
        ConfigureSaveButton();
    }

    public void PauseGame()  => pauseManager?.PauseGame();
    public void ResumeGame() => pauseManager?.ResumeGame();

    // 기존 씬의 BtnHome 클릭 연결을 유지하면서 동작만 저장으로 교체한다.
    public void GoMainMenu() => SaveCurrentStage();

    public void SaveCurrentStage()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (!IsStageScene(currentSceneName))
        {
            Debug.LogWarning(
                $"GameUIManager: 저장할 수 없는 씬입니다: {currentSceneName}"
            );
            return;
        }

        PlayerPrefs.SetString(SavedStageKey, currentSceneName);
        PlayerPrefs.Save();

        ShowSaveMessage();
        Debug.Log($"스테이지 저장 완료: {currentSceneName}");
    }

    public static string GetSavedStageScene()
    {
        string savedSceneName = PlayerPrefs.GetString(
            SavedStageKey,
            "Stage1Scene"
        );

        return IsStageScene(savedSceneName)
            ? savedSceneName
            : "Stage1Scene";
    }

    private void ConfigureSaveButton()
    {
        GameObject saveButtonObject = GameObject.Find("BtnHome");

        if (saveButtonObject == null)
        {
            Debug.LogWarning("GameUIManager: BtnHome을 찾지 못했습니다.");
            return;
        }

        saveButtonObject.name = "BtnSave";

        Image buttonImage = saveButtonObject.GetComponent<Image>();
        Sprite saveIcon = Resources.Load<Sprite>(SaveIconPath);

        if (buttonImage != null && saveIcon != null)
        {
            buttonImage.sprite = saveIcon;
            buttonImage.preserveAspect = true;
        }
        else if (saveIcon == null)
        {
            Debug.LogWarning(
                $"GameUIManager: 저장 아이콘을 찾지 못했습니다: {SaveIconPath}"
            );
        }

        TextMeshProUGUI[] buttonTexts =
            saveButtonObject.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI buttonText in buttonTexts)
            buttonText.gameObject.SetActive(false);

        CreateSaveMessage(saveButtonObject.GetComponentInParent<Canvas>());
    }

    private void CreateSaveMessage(Canvas canvas)
    {
        if (canvas == null || saveMessageText != null)
            return;

        GameObject messageObject = new GameObject(
            "SaveCompleteMessage",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        messageObject.layer = LayerMask.NameToLayer("UI");
        messageObject.transform.SetParent(canvas.transform, false);
        messageObject.transform.SetAsLastSibling();

        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(1f, 1f);
        messageRect.anchorMax = new Vector2(1f, 1f);
        messageRect.pivot = new Vector2(1f, 1f);
        messageRect.anchoredPosition = new Vector2(-18f, -58f);
        messageRect.sizeDelta = new Vector2(380f, 48f);

        saveMessageText = messageObject.GetComponent<TextMeshProUGUI>();
        saveMessageText.text = "게임이 저장되었습니다.";
        saveMessageText.font = FindMaplestoryFont();
        saveMessageText.fontSize = 25f;
        saveMessageText.fontStyle = FontStyles.Bold;
        saveMessageText.color = new Color32(255, 210, 73, 255);
        saveMessageText.alignment = TextAlignmentOptions.Right;
        saveMessageText.raycastTarget = false;
        saveMessageText.outlineColor = new Color32(35, 20, 5, 230);
        saveMessageText.outlineWidth = 0.16f;
        saveMessageText.gameObject.SetActive(false);
    }

    private void ShowSaveMessage()
    {
        if (saveMessageText == null)
            return;

        if (saveMessageCoroutine != null)
            StopCoroutine(saveMessageCoroutine);

        saveMessageCoroutine = StartCoroutine(ShowSaveMessageCoroutine());
    }

    private IEnumerator ShowSaveMessageCoroutine()
    {
        Color messageColor = saveMessageText.color;
        messageColor.a = 1f;
        saveMessageText.color = messageColor;
        saveMessageText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(1.4f);

        const float fadeDuration = 0.35f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            messageColor.a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            saveMessageText.color = messageColor;
            yield return null;
        }

        saveMessageText.gameObject.SetActive(false);
        saveMessageCoroutine = null;
    }

    private static bool IsStageScene(string sceneName)
    {
        return sceneName == "Stage1Scene" ||
               sceneName == "Stage2Scene" ||
               sceneName == "Stage3Scene" ||
               sceneName == "Stage4Scene";
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

        return TMP_Settings.defaultFontAsset;
    }
}
