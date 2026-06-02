using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultSceneManager : MonoBehaviour
{
    public static bool isVictory = false;

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

        // 결과에 따라 사운드 재생
        if (isVictory && victorySound != null)
            audioSource.PlayOneShot(victorySound);
        else if (!isVictory && defeatSound != null)
            audioSource.PlayOneShot(defeatSound);

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        if (isVictory)
            StartCoroutine(FadeIn(victoryPanel));
        else
            StartCoroutine(FadeIn(defeatPanel));
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
        SceneManager.LoadScene("GameScene");
    }

    public void OnClickMainMenu()
    {
        SceneManager.LoadScene("MainScene");
    }
}
