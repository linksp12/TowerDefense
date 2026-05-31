using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Panel")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;

    private bool isPaused = false;

    void Awake()
    {
        // 씬 이동 시 이전 PauseManager 제거
        if (FindObjectsOfType<PauseManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    void Update()
    {
        // pausePanel이 없으면 실행 안 함
        if (pausePanel == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pausePanel == null) return;
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (pausePanel == null) return;
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    public void RestartGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }

    public bool IsPaused => isPaused;
}
