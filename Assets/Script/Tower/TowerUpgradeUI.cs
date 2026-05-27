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
    public Button rapidPathButton;
    public Button piercePathButton;
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
        if (rapidPathButton != null)
            rapidPathButton.onClick.AddListener(OnClickRapidPath);

        if (piercePathButton != null)
            piercePathButton.onClick.AddListener(OnClickPiercePath);

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

        if (rapidPathButton != null)
            rapidPathButton.gameObject.SetActive(needPathSelect);

        if (piercePathButton != null)
            piercePathButton.gameObject.SetActive(needPathSelect);

        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(!needPathSelect && selectedTower.CanUpgrade());
    }

    void OnClickRapidPath()
    {
        if (selectedTower == null) return;

        selectedTower.SelectRapidPath();
        Refresh();
    }

    void OnClickPiercePath()
    {
        if (selectedTower == null) return;

        selectedTower.SelectPiercePath();
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
