using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class ResultSceneManager : MonoBehaviour
{
    public static bool isVictory = false;
    public static string restartSceneName = "Stage1Scene";

    [Header("Result Panels")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    [Header("페이드 설정")]
    public float fadeDuration = 1.0f;

    [Header("사운드")]
    public AudioClip victorySound;
    public AudioClip defeatSound;
    private AudioSource audioSource;

    void Start()
    {
        Time.timeScale = 1f;

        audioSource = GetComponent<AudioSource>();

        if (isVictory && victorySound != null)
            audioSource.PlayOneShot(victorySound);
        else if (!isVictory && defeatSound != null)
            audioSource.PlayOneShot(defeatSound);

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        if (isVictory)
        {
            ConfigureVictoryMessage();
            StartCoroutine(FadeIn(victoryPanel));
        }
        else
            StartCoroutine(FadeIn(defeatPanel));
    }

    private void ConfigureVictoryMessage()
    {
        if (victoryPanel == null)
            return;

        TextMeshProUGUI victoryTitle = null;

        foreach (Transform child in victoryPanel.transform)
        {
            if (child.TryGetComponent(out TextMeshProUGUI text))
            {
                victoryTitle = text;
                break;
            }
        }

        if (victoryTitle == null)
        {
            Debug.LogWarning("ResultSceneManager: 승리 제목을 찾지 못했습니다.");
            return;
        }

        victoryTitle.text = "승리!";
        victoryTitle.color = new Color32(255, 199, 66, 255);
        victoryTitle.fontStyle = FontStyles.Bold;
        victoryTitle.fontSize = 64f;
        victoryTitle.raycastTarget = false;
        victoryTitle.outlineColor = new Color32(65, 36, 12, 220);
        victoryTitle.outlineWidth = 0.18f;

        RectTransform titleRect = victoryTitle.rectTransform;
        titleRect.anchoredPosition = new Vector2(0f, 55f);
        titleRect.sizeDelta = new Vector2(600f, 90f);

        GameObject messageObject = new GameObject(
            "VictoryMessage",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        messageObject.layer = victoryPanel.layer;
        messageObject.transform.SetParent(victoryPanel.transform, false);

        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = new Vector2(0f, -12f);
        messageRect.sizeDelta = new Vector2(700f, 72f);

        TextMeshProUGUI messageText = messageObject.GetComponent<TextMeshProUGUI>();
        messageText.text = "베타테스트가 끝났습니다.\n플레이해주셔서 감사합니다.";
        messageText.font = victoryTitle.font;
        messageText.fontSize = 25f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.color = new Color32(245, 235, 211, 255);
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.lineSpacing = 10f;
        messageText.raycastTarget = false;
        messageText.outlineColor = new Color32(20, 14, 10, 210);
        messageText.outlineWidth = 0.12f;
    }

    IEnumerator FadeIn(GameObject panel)
    {
        if (panel == null) yield break;
        panel.SetActive(true);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    public void OnClickRestart()
    {
        if (Application.CanStreamedLevelBeLoaded(restartSceneName))
        {
            SceneManager.LoadScene(restartSceneName);
        }
        else
        {
            Debug.LogWarning(
                $"재시작할 씬을 찾을 수 없습니다: {restartSceneName}. Stage1Scene으로 이동합니다."
            );

            SceneManager.LoadScene("Stage1Scene");
        }
    }

    public void OnClickMainMenu()
    {
        SceneManager.LoadScene("MainScene");
    }
}
