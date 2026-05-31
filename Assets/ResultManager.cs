using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class ResultManager : MonoBehaviour
{
    public static ResultManager Instance { get; private set; }

    [Header("Result Panel")]
    public GameObject resultPanel;
    public CanvasGroup resultCanvasGroup;

    [Header("텍스트")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subText;

    [Header("버튼")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("페이드 설정")]
    public float fadeDuration = 1.0f;
    public float delayBeforeFade = 0.5f;

    void Awake()
    {
        Instance = this;
        resultPanel.SetActive(false);
    }

    void Start()
    {
        restartButton.onClick.AddListener(RestartGame);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    // 승리 시 호출
    public void ShowVictory()
    {
        titleText.text = "승리!";
        titleText.color = new Color(1f, 0.85f, 0f); // 금색
        subText.text = "모든 웨이브를 막아냈습니다!";
        StartCoroutine(ShowResult());
    }

    // 패배 시 호출
    public void ShowDefeat()
    {
        titleText.text = "패배...";
        titleText.color = new Color(1f, 0.3f, 0.3f); // 빨간색
        subText.text = "기지가 함락되었습니다.";
        StartCoroutine(ShowResult());
    }

    IEnumerator ShowResult()
    {
        // 잠깐 대기 후 패널 표시
        yield return new WaitForSecondsRealtime(delayBeforeFade);

        resultPanel.SetActive(true);
        Time.timeScale = 0f;

        // 페이드 인 애니메이션
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            resultCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        resultCanvasGroup.alpha = 1f;
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene"); // 메인씬 이름 확인!
    }
}
