using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerUpgradeUI : MonoBehaviour
{
    public static TowerUpgradeUI Instance;

    [Header("Panel")]
    public GameObject panel;

    [Header("Texts")]
    public TextMeshProUGUI towerNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;

    [Header("Buttons")]
    public Button pathAButton;
    public Button pathBButton;
    public Button upgradeButton;
    public Button closeButton;

    private TowerUpgrade selectedTower;

    void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    void Start()
    {
        if (pathAButton != null)
            pathAButton.onClick.AddListener(OnClickPathA);

        if (pathBButton != null)
            pathBButton.onClick.AddListener(OnClickPathB);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnClickUpgrade);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Open(TowerUpgrade tower)
    {
        selectedTower = tower;

        if (panel != null)
            panel.SetActive(true);

        Refresh();
    }

    void Refresh()
    {
        if (selectedTower == null) return;

        if (towerNameText != null)
            towerNameText.text = selectedTower.GetTowerName();

        if (levelText != null)
            levelText.text = "Lv." + selectedTower.level;

        if (costText != null)
        {
            if (selectedTower.CanUpgrade())
                costText.text = "비용: " + selectedTower.GetUpgradeCost() + "G";
            else
                costText.text = "최대 레벨";
        }

        bool needPathSelect = selectedTower.path == TowerUpgrade.UpgradePath.None;

        if (pathAButton != null)
        {
            pathAButton.gameObject.SetActive(needPathSelect);

            TextMeshProUGUI buttonText = pathAButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = selectedTower.GetPathAName();
        }

        if (pathBButton != null)
        {
            pathBButton.gameObject.SetActive(needPathSelect);

            TextMeshProUGUI buttonText = pathBButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = selectedTower.GetPathBName();
        }

        if (upgradeButton != null)
        {
            upgradeButton.gameObject.SetActive(!needPathSelect && selectedTower.CanUpgrade());
        }
    }

    void OnClickPathA()
    {
        if (selectedTower == null) return;

        selectedTower.SelectPathA();
        Refresh();
    }

    void OnClickPathB()
    {
        if (selectedTower == null) return;

        selectedTower.SelectPathB();
        Refresh();
    }

    void OnClickUpgrade()
    {
        if (selectedTower == null) return;

        selectedTower.Upgrade();
        Refresh();
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);

        selectedTower = null;
    }
}
