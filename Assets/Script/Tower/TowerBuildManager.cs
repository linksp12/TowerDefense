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

    [Header("Tower Costs")]
    public int basicTowerCost = 50;   // 기본 타워 가격
    public int cannonTowerCost = 100; // 캐논 타워 가격
    public int magicTowerCost = 150;  // 마법 타워 가격

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
            return;
        }

        if (towerPrefab == null)
        {
            Debug.LogWarning("타워 프리팹이 연결되지 않았습니다.");
            return;
        }

        // 돈 부족 체크 — SpendMoney가 false면 설치 취소
        if (GameManager.Instance != null && !GameManager.Instance.SpendMoney(cost))
        {
            Debug.Log($"돈이 부족합니다! 필요: {cost}G");
            // 필요 시 여기서 "돈 부족" UI 메시지 표시 가능
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