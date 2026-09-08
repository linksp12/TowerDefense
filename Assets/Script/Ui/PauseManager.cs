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

    [Header("Pause Visual")]
    public Sprite pauseBackgroundSprite;
    public Vector2 pauseBoxSize = new Vector2(760f, 430f);
    [Range(0f, 1f)] public float backdropAlpha = 0.72f;

    private bool isPaused = false;

    void Awake()
    {
        ConfigurePauseVisual();
        pausePanel.SetActive(false);

        resumeButton.onClick.AddListener(ResumeGame);
        restartButton.onClick.AddListener(RestartGame);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    void Update()
    {
        if (pausePanel == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        DamagePopup.HideAll();
        CloseGameplayPanels();
        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();
    }

    private void ConfigurePauseVisual()
    {
        if (pausePanel == null)
            return;

        Image backdrop = pausePanel.GetComponent<Image>();
        if (backdrop != null)
        {
            backdrop.sprite = null;
            backdrop.color = new Color(0f, 0f, 0f, backdropAlpha);
            backdrop.raycastTarget = true;
        }

        Transform popupTransform = pausePanel.transform.Find("PopupBox");
        if (popupTransform == null)
            return;

        RectTransform popupRect = popupTransform as RectTransform;
        if (popupRect != null)
        {
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.anchoredPosition = Vector2.zero;
            popupRect.sizeDelta = pauseBoxSize;
        }

        Image popupImage = popupTransform.GetComponent<Image>();
        if (popupImage != null && pauseBackgroundSprite != null)
        {
            popupImage.sprite = pauseBackgroundSprite;
            popupImage.type = Image.Type.Sliced;
            popupImage.preserveAspect = false;
            popupImage.color = Color.white;
            popupImage.raycastTarget = false;
        }
    }

    private void CloseGameplayPanels()
    {
        if (TowerBuildManager.Instance != null)
            TowerBuildManager.Instance.CloseBuildPanelInstantly();

        if (TowerUpgradeUI.Instance != null)
            TowerUpgradeUI.Instance.CloseInstantly();

        if (BossInfoUI.Instance != null)
            BossInfoUI.Instance.SetSuppressedByTowerPanel(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        GameSpeedController.ResumeSelectedSpeed();
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

    // 창 없이 게임만 멈추기/재개 (II 버튼용)
    public void ToggleTimeScale()
    {
        if (Time.timeScale == 0f)
            GameSpeedController.ResumeSelectedSpeed();
        else
            Time.timeScale = 0f;
    }

    public bool IsPaused => isPaused;
}
