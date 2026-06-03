using UnityEngine;

public class TowerBuildManager : MonoBehaviour
{
    public static TowerBuildManager Instance;

    [Header("UI")]
    public GameObject towerBuildPanel;
    public PanelAnimator towerBuildPanelAnimator;

    [Header("Panel Position")]
    public RectTransform towerBuildPanelRect;
    public Canvas canvas;
    public Vector2 panelOffset = new Vector2(0f, 120f);

    [Header("Effects")]
    public PanelShake towerBuildPanelShake;

    [Header("Tower Prefabs")]
    public GameObject basicTowerPrefab;
    public GameObject cannonTowerPrefab;
    public GameObject magicTowerPrefab;

    [Header("Tower Costs")]
    public int basicTowerCost = 50;
    public int cannonTowerCost = 100;
    public int magicTowerCost = 150;

    private BuildPoint selectedBuildPoint;

    private void Awake()
    {
        Instance = this;

        if (towerBuildPanelAnimator == null && towerBuildPanel != null)
            towerBuildPanelAnimator = towerBuildPanel.GetComponent<PanelAnimator>();

        if (towerBuildPanelRect == null && towerBuildPanel != null)
            towerBuildPanelRect = towerBuildPanel.GetComponent<RectTransform>();

        if (towerBuildPanelShake == null && towerBuildPanel != null)
            towerBuildPanelShake = towerBuildPanel.GetComponent<PanelShake>();

        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
    }

    private void Start()
    {
        if (towerBuildPanelAnimator != null)
        {
            towerBuildPanelAnimator.HideInstant();
        }
        else if (towerBuildPanel != null)
        {
            towerBuildPanel.SetActive(false);
        }
    }

    public void OpenBuildPanel(BuildPoint buildPoint)
    {
        if (buildPoint == null)
            return;

        selectedBuildPoint = buildPoint;

        MovePanelToBuildPoint(buildPoint.transform.position);

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayOpenPanel();

        if (towerBuildPanelAnimator != null)
        {
            towerBuildPanelAnimator.Show();
        }
        else if (towerBuildPanel != null)
        {
            towerBuildPanel.SetActive(true);
        }
    }

    private void MovePanelToBuildPoint(Vector3 worldPosition)
    {
        if (towerBuildPanelRect == null || canvas == null || Camera.main == null)
        {
            Debug.LogWarning("설치 패널 위치 이동에 필요한 값이 연결되지 않았습니다.");
            return;
        }

        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 uiPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out uiPosition
        );

        Vector2 targetPosition = uiPosition + panelOffset;

        float panelHalfWidth = towerBuildPanelRect.rect.width * 0.5f;
        float panelHalfHeight = towerBuildPanelRect.rect.height * 0.5f;

        float canvasHalfWidth = canvasRect.rect.width * 0.5f;
        float canvasHalfHeight = canvasRect.rect.height * 0.5f;

        targetPosition.x = Mathf.Clamp(
            targetPosition.x,
            -canvasHalfWidth + panelHalfWidth,
            canvasHalfWidth - panelHalfWidth
        );

        targetPosition.y = Mathf.Clamp(
            targetPosition.y,
            -canvasHalfHeight + panelHalfHeight,
            canvasHalfHeight - panelHalfHeight
        );

        towerBuildPanelRect.anchoredPosition = targetPosition;
    }

    public void BuildBasicTower()
    {
        BuildTower(basicTowerPrefab, basicTowerCost);
    }

    public void BuildCannonTower()
    {
        BuildTower(cannonTowerPrefab, cannonTowerCost);
    }

    public void BuildMagicTower()
    {
        BuildTower(magicTowerPrefab, magicTowerCost);
    }

    private void BuildTower(GameObject towerPrefab, int cost)
    {
        if (selectedBuildPoint == null)
        {
            Debug.Log("선택된 설치 위치가 없습니다.");
            PlayFailFeedback();
            return;
        }

        if (towerPrefab == null)
        {
            Debug.LogWarning("타워 프리팹이 연결되지 않았습니다.");
            PlayFailFeedback();
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.SpendMoney(cost))
        {
            Debug.Log($"돈이 부족합니다! 필요: {cost}G");
            PlayFailFeedback();
            return;
        }

        selectedBuildPoint.BuildTower(towerPrefab);

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayBuildSuccess();

        CloseBuildPanel(false);
    }

    public void CloseBuildPanel()
    {
        CloseBuildPanel(true);
    }

    private void CloseBuildPanel(bool playSound)
    {
        if (playSound && UISoundManager.Instance != null)
            UISoundManager.Instance.PlayClosePanel();

        if (towerBuildPanelAnimator != null)
        {
            towerBuildPanelAnimator.Hide();
        }
        else if (towerBuildPanel != null)
        {
            towerBuildPanel.SetActive(false);
        }

        selectedBuildPoint = null;
    }

    private void PlayFailFeedback()
    {
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayFail();

        if (towerBuildPanelShake != null)
            towerBuildPanelShake.PlayShake();
    }

    public int GetCheapestTowerCost()
    {
        int cheapest = basicTowerCost;

        if (cannonTowerCost < cheapest)
            cheapest = cannonTowerCost;

        if (magicTowerCost < cheapest)
            cheapest = magicTowerCost;

        return cheapest;
    }
}