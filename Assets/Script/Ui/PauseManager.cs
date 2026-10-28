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
    public Button plazaButton;

    [Header("Pause Visual")]
    public Sprite pauseBackgroundSprite;
    public Vector2 pauseBoxSize = new Vector2(760f, 430f);
    [Range(0f, 1f)] public float backdropAlpha = 0.72f;

    private const float PauseBoxHeightWithPlaza = 500f;

    private bool isPaused = false;
    // 도트 디자인 일시정지 창. 있으면 기존 패널 대신 이 창을 보여 준다.
    private PauseMenuUI pauseMenu;

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
        pauseMenu = PauseMenuUI.Create(this, pausePanel.GetComponentInParent<Canvas>(true));

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (plazaButton != null)
            plazaButton.onClick.AddListener(GoToPlaza);
    }

    private void CreateSharedPausePanel()
    {
        if (pausePanelPrefab == null)
            return;

        GameObject scenePausePanel = pausePanel;
        Canvas canvas = scenePausePanel != null
            ? scenePausePanel.GetComponentInParent<Canvas>()
            : null;

        if (!IsSceneUiCanvas(canvas))
        {
            foreach (Canvas candidate in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (IsSceneUiCanvas(candidate))
                {
                    canvas = candidate;
                    break;
                }
            }
        }

        if (!IsSceneUiCanvas(canvas))
        {
            Debug.LogWarning("PauseManager: 현재 씬에 클릭 가능한 UI Canvas가 없습니다.");
            return;
        }

        pausePanel = Instantiate(pausePanelPrefab, canvas.transform);
        pausePanel.name = "PausePanel";

        if (scenePausePanel != null)
            scenePausePanel.SetActive(false);
    }

    private bool IsSceneUiCanvas(Canvas candidate)
    {
        if (candidate == null || !candidate.isActiveAndEnabled ||
            candidate.gameObject.scene != gameObject.scene ||
            candidate.renderMode == RenderMode.WorldSpace)
            return false;

        GraphicRaycaster raycaster = candidate.GetComponent<GraphicRaycaster>();
        return raycaster != null && raycaster.isActiveAndEnabled;
    }

    private void FindPauseButtons()
    {
        resumeButton = FindButton("PopupBox/ResumeButton", resumeButton);
        restartButton = FindButton("PopupBox/RestartButton", restartButton);
        mainMenuButton = FindButton("PopupBox/MainMenuButton", mainMenuButton);
        plazaButton = FindButton("PopupBox/PlazaButton", plazaButton);
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

        // 결과창이 뜬 뒤에는 ESC로 일시정지/재개하지 않는다(재개 시 배속이 되살아나 결과창 뒤 전투가 다시 돈다).
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded()) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 그만두기 확인 중이면 확인창만 닫는다.
            if (isPaused && pauseMenu != null && pauseMenu.IsConfirming)
            {
                pauseMenu.CancelConfirm();
                return;
            }
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
        if (pauseMenu != null)
        {
            pauseMenu.Show();
            return;
        }
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
            // 버튼 4개(광장으로 포함)가 틀 안에 들어가도록 높이를 보장한다. 틀 그림 위아래에 투명 여백이 있다.
            popupRect.sizeDelta = new Vector2(pauseBoxSize.x,
                plazaButton != null ? Mathf.Max(pauseBoxSize.y, PauseBoxHeightWithPlaza) : pauseBoxSize.y);
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
        if (pauseMenu != null)
            pauseMenu.Hide();
    }

    // 결과창을 띄울 때 일시정지 창만 닫는다. 시간 배율은 결과 처리 쪽이 정한다.
    public void CloseForResult()
    {
        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (pauseMenu != null)
            pauseMenu.Hide();
    }

    // 다시하기·메인으로도 광장으로와 같은 로딩 화면을 쓴다(시간 배율은 로딩 화면이 1로 돌린다).
    public void RestartGame()
    {
        if (SceneLoadingScreen.IsLoading)
            return;
        if (SceneLoadingScreen.TryLoad(SceneManager.GetActiveScene().name, "전장을 다시 준비하고 있습니다"))
            isPaused = false;
    }

    public void GoToMainMenu()
    {
        if (SceneLoadingScreen.IsLoading)
            return;
        if (SceneLoadingScreen.TryLoad("MainScene", "메인 화면으로 이동 중"))
            isPaused = false;
    }

    // 전투를 그만두고 광장으로 돌아간다. 결과창의 "광장으로"와 같은 로딩 화면을 쓴다.
    public void GoToPlaza()
    {
        if (SceneLoadingScreen.IsLoading)
            return;

        // 시간 배율은 로딩 화면이 로드 시작 때 1로 돌리고, 실패하면 이전 값(일시정지 0)으로 되돌린다.
        if (plazaButton != null)
            plazaButton.interactable = false;
        if (!SceneLoadingScreen.TryLoad("PlazaScene", "광장으로 이동 중", RestorePlazaButton))
            RestorePlazaButton();
    }

    private void RestorePlazaButton()
    {
        if (this != null && plazaButton != null)
            plazaButton.interactable = true;
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
