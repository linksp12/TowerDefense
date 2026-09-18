using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Panel")]
    public GameObject pausePanelPrefab;
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
        CreateSharedPausePanel();

        if (pausePanel == null)
        {
            Debug.LogWarning("PauseManager: 일시정지 패널을 찾지 못했습니다.");
            return;
        }

        FindPauseButtons();
        ConfigurePauseVisual();
        pausePanel.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private void CreateSharedPausePanel()
    {
        if (pausePanelPrefab == null)
            return;

        GameObject scenePausePanel = pausePanel;
        Canvas canvas = scenePausePanel != null
            ? scenePausePanel.GetComponentInParent<Canvas>()
            : FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("PauseManager: 일시정지 패널을 둘 Canvas가 없습니다.");
            return;
        }

        pausePanel = Instantiate(pausePanelPrefab, canvas.transform);
        pausePanel.name = "PausePanel";

        if (scenePausePanel != null)
            scenePausePanel.SetActive(false);
    }

    private void FindPauseButtons()
    {
        resumeButton = FindButton("PopupBox/ResumeButton", resumeButton);
        restartButton = FindButton("PopupBox/RestartButton", restartButton);
        mainMenuButton = FindButton("PopupBox/MainMenuButton", mainMenuButton);
    }

    private Button FindButton(string path, Button fallback)
    {
        Transform buttonTransform = pausePanel.transform.Find(path);
        return buttonTransform != null
            ? buttonTransform.GetComponent<Button>()
            : fallback;
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
