using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerUpgradeUI : MonoBehaviour
{
    public static TowerUpgradeUI Instance;

    [Header("Panel")]
    public GameObject panel;
    public Image panelImage;

    [Header("Animation")]
    public PanelAnimator panelAnimator;

    [Header("Panel Position")]
    public RectTransform panelRect;
    public Canvas canvas;
    public Vector2 panelOffset = new Vector2(0f, 130f);

    [Header("Panel Sprites")]
    public Sprite twoPathPanelSprite;
    public Sprite onePathPanelSprite;

    [Header("Texts")]
    public TextMeshProUGUI towerNameText;

    [Header("Path A")]
    public Button pathAButton;
    public Image pathAImage;
    public TextMeshProUGUI pathANameText;

    [Header("Path B")]
    public Button pathBButton;
    public Image pathBImage;
    public TextMeshProUGUI pathBNameText;

    [Header("Final Path")]
    public Button finalPathButton;
    public Image finalUpgradeImage;
    public TextMeshProUGUI finalUpgradeNameText;

    [Header("Sell")]
    public Button sellButton;
    public TextMeshProUGUI sellPriceText;

    [Header("Close")]
    public Button closeButton;

    [Header("Tooltip")]
    public UpgradeTooltipUI tooltipUI;

    [Header("Shake")]
    public PanelShake panelShake;

    [Header("Button Position")]
    public RectTransform sellButtonRect;
    public RectTransform closeButtonRect;

    [Header("Two Path Panel Button Position")]
    public Vector2 twoPathSellPosition = new Vector2(315f, -70f);
    public Vector2 twoPathClosePosition = new Vector2(335f, 120f);

    [Header("One Path Panel Button Position")]
    public Vector2 onePathSellPosition = new Vector2(300f, -70f);
    public Vector2 onePathClosePosition = new Vector2(300f, 120f);

    private TowerUpgrade selectedTower;

    void Awake()
    {
        Instance = this;

        if (panelAnimator == null && panel != null)
            panelAnimator = panel.GetComponent<PanelAnimator>();

        if (panelRect == null && panel != null)
            panelRect = panel.GetComponent<RectTransform>();

        if (panelShake == null && panel != null)
            panelShake = panel.GetComponent<PanelShake>();

        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

        if (panelAnimator != null)
            panelAnimator.HideInstant();
        else if (panel != null)
            panel.SetActive(false);
    }

    void Start()
    {
        if (pathAButton != null)
            pathAButton.onClick.AddListener(OnClickPathA);

        if (pathBButton != null)
            pathBButton.onClick.AddListener(OnClickPathB);

        if (finalPathButton != null)
            finalPathButton.onClick.AddListener(OnClickFinalPath);

        if (sellButton != null)
            sellButton.onClick.AddListener(OnClickSell);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);

        if (sellButtonRect == null && sellButton != null)
            sellButtonRect = sellButton.GetComponent<RectTransform>();

        if (closeButtonRect == null && closeButton != null)
            closeButtonRect = closeButton.GetComponent<RectTransform>();
    }

    public void Open(TowerUpgrade tower)
    {
        selectedTower = tower;

        MovePanelToTowerPosition();
        Refresh();
        HideTooltip();

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayOpenPanel();

        if (panelAnimator != null)
            panelAnimator.Show();
        else if (panel != null)
            panel.SetActive(true);
    }

    void MovePanelToTowerPosition()
    {
        if (selectedTower == null)
            return;

        if (panelRect == null || canvas == null || Camera.main == null)
        {
            Debug.LogWarning("업그레이드 패널 위치 이동에 필요한 값이 연결되지 않았습니다.");
            return;
        }

        Vector3 worldPosition = selectedTower.transform.position;

        BuildPoint ownerBuildPoint = selectedTower.GetOwnerBuildPoint();
        if (ownerBuildPoint != null)
            worldPosition = ownerBuildPoint.transform.position;

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

        float panelHalfWidth = panelRect.rect.width * 0.5f;
        float panelHalfHeight = panelRect.rect.height * 0.5f;

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

        panelRect.anchoredPosition = targetPosition;
    }

    void Refresh()
    {
        if (selectedTower == null) return;

        if (towerNameText != null)
            towerNameText.text = selectedTower.GetTowerName() + "  Lv." + selectedTower.level;

        if (selectedTower.level == 1)
            ShowTwoPathMode();
        else if (selectedTower.level == 2)
            ShowFinalPathMode();
        else
            ShowMaxLevelMode();

        RefreshSellPriceText();
    }

    void ShowTwoPathMode()
    {
        if (panelImage != null && twoPathPanelSprite != null)
            panelImage.sprite = twoPathPanelSprite;

        if (pathAButton != null)
            pathAButton.gameObject.SetActive(true);

        if (pathBButton != null)
            pathBButton.gameObject.SetActive(true);

        if (finalPathButton != null)
            finalPathButton.gameObject.SetActive(false);

        if (pathAImage != null)
            pathAImage.sprite = selectedTower.GetPathASprite();

        if (pathBImage != null)
            pathBImage.sprite = selectedTower.GetPathBSprite();

        if (pathANameText != null)
            pathANameText.text = selectedTower.GetPathAName();

        if (pathBNameText != null)
            pathBNameText.text = selectedTower.GetPathBName();

        if (sellButton != null)
            sellButton.gameObject.SetActive(true);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        ApplyButtonPositions(twoPathSellPosition, twoPathClosePosition);
    }

    void ShowFinalPathMode()
    {
        if (panelImage != null && onePathPanelSprite != null)
            panelImage.sprite = onePathPanelSprite;

        if (pathAButton != null)
            pathAButton.gameObject.SetActive(false);

        if (pathBButton != null)
            pathBButton.gameObject.SetActive(false);

        if (finalPathButton != null)
            finalPathButton.gameObject.SetActive(true);

        if (finalUpgradeImage != null)
            finalUpgradeImage.sprite = selectedTower.GetFinalUpgradeSprite();

        if (finalUpgradeNameText != null)
            finalUpgradeNameText.text = selectedTower.GetFinalUpgradeName();

        if (sellButton != null)
            sellButton.gameObject.SetActive(true);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        ApplyButtonPositions(onePathSellPosition, onePathClosePosition);
    }

    void ShowMaxLevelMode()
    {
        if (panelImage != null && onePathPanelSprite != null)
            panelImage.sprite = onePathPanelSprite;

        if (pathAButton != null)
            pathAButton.gameObject.SetActive(false);

        if (pathBButton != null)
            pathBButton.gameObject.SetActive(false);

        if (finalPathButton != null)
            finalPathButton.gameObject.SetActive(false);

        if (towerNameText != null)
            towerNameText.text = selectedTower.GetTowerName() + "  최대 레벨";

        if (sellButton != null)
            sellButton.gameObject.SetActive(true);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        ApplyButtonPositions(onePathSellPosition, onePathClosePosition);
    }

    void ApplyButtonPositions(Vector2 sellPosition, Vector2 closePosition)
    {
        if (sellButtonRect != null)
            sellButtonRect.anchoredPosition = sellPosition;

        if (closeButtonRect != null)
            closeButtonRect.anchoredPosition = closePosition;
    }

    void RefreshSellPriceText()
    {
        if (sellPriceText != null && selectedTower != null)
            sellPriceText.text = selectedTower.GetSellPrice() + "G";
    }

    public void ShowTooltip(UpgradeTooltipTrigger.TooltipType type)
    {
        if (selectedTower == null || tooltipUI == null)
            return;

        string title = "";
        string desc = "";

        if (type == UpgradeTooltipTrigger.TooltipType.PathA)
        {
            title = selectedTower.GetPathAName();
            desc = GetPathADescription();
        }
        else if (type == UpgradeTooltipTrigger.TooltipType.PathB)
        {
            title = selectedTower.GetPathBName();
            desc = GetPathBDescription();
        }
        else if (type == UpgradeTooltipTrigger.TooltipType.FinalUpgrade)
        {
            title = selectedTower.GetFinalUpgradeName();
            desc = GetFinalUpgradeDescription();
        }
        else if (type == UpgradeTooltipTrigger.TooltipType.Sell)
        {
            title = "타워 판매";
            desc =
                "현재 타워를 판매합니다.\n\n" +
                "판매가: " + selectedTower.GetSellPrice() + "G\n" +
                "판매 후 해당 위치에 다시 타워를 설치할 수 있습니다.";
        }

        tooltipUI.Show(title, desc);
    }

    public void HideTooltip()
    {
        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    string GetPathADescription()
    {
        if (selectedTower.towerType == TowerUpgrade.TowerType.Archer)
        {
            return
                "빠른 공격 속도를 강화하는 루트입니다.\n\n" +
                "비용: " + selectedTower.level2Cost + "G\n" +
                "특징: 공격속도 증가\n" +
                "추천: 많은 몬스터 처리";
        }

        if (selectedTower.towerType == TowerUpgrade.TowerType.Cannon)
        {
            return
                "폭발 피해를 강화하는 루트입니다.\n\n" +
                "비용: " + selectedTower.level2Cost + "G\n" +
                "특징: 주변 몬스터에게 범위 피해\n" +
                "추천: 몰려오는 몬스터 처리";
        }

        if (selectedTower.towerType == TowerUpgrade.TowerType.Magic)
        {
            return
                "마법화살을 강화하는 루트입니다.\n\n" +
                "비용: " + selectedTower.level2Cost + "G\n" +
                "특징: 몬스터 이동속도 감소\n" +
                "추천: 적 이동 지연";
        }

        return "업그레이드 정보를 불러올 수 없습니다.";
    }

    string GetPathBDescription()
    {
        if (selectedTower.towerType == TowerUpgrade.TowerType.Archer)
        {
            return
                "강한 화살과 관통 공격을 강화하는 루트입니다.\n\n" +
                "비용: " + selectedTower.level2Cost + "G\n" +
                "특징: 높은 공격력 / 관통 공격\n" +
                "추천: 체력이 높은 몬스터 처리";
        }

        if (selectedTower.towerType == TowerUpgrade.TowerType.Cannon)
        {
            return
                "강력한 한 방 피해를 강화하는 루트입니다.\n\n" +
                "비용: " + selectedTower.level2Cost + "G\n" +
                "특징: 높은 단일 피해\n" +
                "추천: 보스 및 단단한 몬스터 처리";
        }

        if (selectedTower.towerType == TowerUpgrade.TowerType.Magic)
        {
            return
                "화염 피해를 강화하는 루트입니다.\n\n" +
                "비용: " + selectedTower.level2Cost + "G\n" +
                "특징: 지속 피해 부여\n" +
                "추천: 체력이 높은 몬스터 처리";
        }

        return "업그레이드 정보를 불러올 수 없습니다.";
    }

    string GetFinalUpgradeDescription()
    {
        string effectText = "";

        if (selectedTower.towerType == TowerUpgrade.TowerType.Archer)
        {
            if (selectedTower.path == TowerUpgrade.UpgradePath.PathA)
                effectText = "공격속도가 크게 증가합니다.";
            else if (selectedTower.path == TowerUpgrade.UpgradePath.PathB)
                effectText = "관통 성능과 공격력이 크게 증가합니다.";
        }
        else if (selectedTower.towerType == TowerUpgrade.TowerType.Cannon)
        {
            if (selectedTower.path == TowerUpgrade.UpgradePath.PathA)
                effectText = "폭발 범위와 피해가 크게 증가합니다.";
            else if (selectedTower.path == TowerUpgrade.UpgradePath.PathB)
                effectText = "강력한 공성 피해를 입힙니다.";
        }
        else if (selectedTower.towerType == TowerUpgrade.TowerType.Magic)
        {
            if (selectedTower.path == TowerUpgrade.UpgradePath.PathA)
                effectText = "마법화살의 슬로우 효과가 강화됩니다.";
            else if (selectedTower.path == TowerUpgrade.UpgradePath.PathB)
                effectText = "화염 지속 피해가 강화됩니다.";
        }

        return
            "최종 단계로 업그레이드합니다.\n\n" +
            "비용: " + selectedTower.level3Cost + "G\n" +
            "효과: " + effectText;
    }

    void OnClickPathA()
    {
        if (selectedTower == null) return;

        int beforeLevel = selectedTower.level;

        selectedTower.SelectPathA();

        if (selectedTower.level > beforeLevel)
        {
            PlayUpgradeSuccessSound();
            Close();
        }
        else
        {
            PlayFailFeedback();
            Refresh();
        }
    }

    void OnClickPathB()
    {
        if (selectedTower == null) return;

        int beforeLevel = selectedTower.level;

        selectedTower.SelectPathB();

        if (selectedTower.level > beforeLevel)
        {
            PlayUpgradeSuccessSound();
            Close();
        }
        else
        {
            PlayFailFeedback();
            Refresh();
        }
    }

    void OnClickFinalPath()
    {
        if (selectedTower == null) return;

        int beforeLevel = selectedTower.level;

        selectedTower.Upgrade();

        if (selectedTower.level > beforeLevel)
        {
            PlayUpgradeSuccessSound();
            Close();
        }
        else
        {
            PlayFailFeedback();
            Refresh();
        }
    }

    void OnClickSell()
    {
        if (selectedTower == null) return;

        int sellPrice = selectedTower.GetSellPrice();

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlaySell();

        if (GameManager.Instance != null)
            GameManager.Instance.AddMoney(sellPrice);

        BuildPoint ownerBuildPoint = selectedTower.GetOwnerBuildPoint();

        if (ownerBuildPoint != null)
            ownerBuildPoint.ClearTower();

        Destroy(selectedTower.gameObject);

        Close();
    }

    void OnClickClose()
    {
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayClosePanel();

        Close();
    }

    void PlayUpgradeSuccessSound()
    {
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayUpgradeSuccess();
    }

    void PlayFailFeedback()
    {
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayFail();

        if (panelShake != null)
            panelShake.PlayShake();
    }

    public void Close()
    {
        HideTooltip();

        if (panelAnimator != null)
            panelAnimator.Hide();
        else if (panel != null)
            panel.SetActive(false);

        selectedTower = null;
    }
}