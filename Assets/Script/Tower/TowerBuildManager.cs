using UnityEngine;

public class TowerBuildManager : MonoBehaviour
{
    public static TowerBuildManager Instance;

    [Header("UI")]
    public GameObject towerBuildPanel;

    [Header("Tower Prefabs")]
    public GameObject basicTowerPrefab;
    public GameObject cannonTowerPrefab;
    public GameObject magicTowerPrefab;

    private BuildPoint selectedBuildPoint;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        towerBuildPanel.SetActive(false);
    }

    public void OpenBuildPanel(BuildPoint buildPoint)
    {
        selectedBuildPoint = buildPoint;
        towerBuildPanel.SetActive(true);
    }

    public void BuildBasicTower()
    {
        BuildTower(basicTowerPrefab);
    }

    public void BuildCannonTower()
    {
        BuildTower(cannonTowerPrefab);
    }

    public void BuildMagicTower()
    {
        BuildTower(magicTowerPrefab);
    }

    private void BuildTower(GameObject towerPrefab)
    {
        if (selectedBuildPoint == null)
        {
            Debug.Log("선택된 설치 위치가 없습니다.");
            return;
        }

        if (towerPrefab == null)
        {
            Debug.LogWarning("타워 프리팹이 연결되지 않았습니다.");
            return;
        }

        selectedBuildPoint.BuildTower(towerPrefab);
        towerBuildPanel.SetActive(false);
        selectedBuildPoint = null;
    }

    public void CloseBuildPanel()
    {
        towerBuildPanel.SetActive(false);
        selectedBuildPoint = null;
    }
}
