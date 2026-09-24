using UnityEngine;

public class TowerUpgrade : MonoBehaviour
{
    public enum UpgradePath
    {
        None,
        PathA,
        PathB
    }

    [Header("현재 상태")]
    public int level = 1;
    public int maxLevel = 3;
    public UpgradePath path = UpgradePath.None;

    [Header("타워 이미지")]
    public Sprite basicTowerSprite;
    public Sprite pathALv2TowerSprite;
    public Sprite pathALv3TowerSprite;
    public Sprite pathBLv2TowerSprite;
    public Sprite pathBLv3TowerSprite;

    [Header("발사체 프리팹")]
    public GameObject basicProjectilePrefab;
    public GameObject pathALv2ProjectilePrefab;
    public GameObject pathALv3ProjectilePrefab;
    public GameObject pathBLv2ProjectilePrefab;
    public GameObject pathBLv3ProjectilePrefab;

    private SpriteRenderer spriteRenderer;
    private TowerAttack towerAttack;

    private BuildPoint ownerBuildPoint;

    private TowerData Data => towerAttack != null ? towerAttack.towerData : null;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        towerAttack = GetComponent<TowerAttack>();
    }

    void Start()
    {
        ApplyBasicSetting();
    }

    void ApplyBasicSetting()
    {
        if (spriteRenderer != null && basicTowerSprite != null)
        {
            spriteRenderer.sprite = basicTowerSprite;
        }

        if (towerAttack != null && basicProjectilePrefab != null)
        {
            towerAttack.arrowPrefab = basicProjectilePrefab;
        }
    }

    public void SetOwnerBuildPoint(BuildPoint buildPoint)
    {
        ownerBuildPoint = buildPoint;
    }

    public BuildPoint GetOwnerBuildPoint()
    {
        return ownerBuildPoint;
    }

    public bool CanUpgrade()
    {
        return level < maxLevel;
    }

    public int GetUpgradeCost()
    {
        TowerData data = Data;

        if (data == null)
            return 0;

        if (level == 1) return data.level2Cost;
        if (level == 2) return data.level3Cost;
        return 0;
    }

    public int GetBaseCost()
    {
        if (towerAttack == null)
            towerAttack = GetComponent<TowerAttack>();

        if (towerAttack == null)
        {
            Debug.LogWarning($"{name}: TowerAttack가 없어 판매가를 계산할 수 없습니다.", this);
            return 0;
        }

        return towerAttack.BuildCost;
    }

    public int GetTotalUsedCost()
    {
        int totalCost = GetBaseCost();

        TowerData data = Data;

        if (data == null)
            return totalCost;

        if (level >= 2)
            totalCost += data.level2Cost;

        if (level >= 3)
            totalCost += data.level3Cost;

        return totalCost;
    }

    public int GetSellPrice()
    {
        TowerData data = Data;
        float sellRate = data != null ? data.sellRate : 0f;
        return Mathf.RoundToInt(GetTotalUsedCost() * sellRate);
    }

    public string GetStatText()
    {
        if (towerAttack == null)
        {
            return "능력치 정보를 불러올 수 없습니다.";
        }

        return
            "공격력: " + towerAttack.damage + "\n" +
            "공격속도: " + towerAttack.attackCooldown + "초\n" +
            "사거리: " + towerAttack.attackRange + "\n" +
            "판매가: " + GetSellPrice() + "G";
    }

    public void SelectPathA()
    {
        SelectPathAndUpgrade(UpgradePath.PathA);
    }

    public void SelectPathB()
    {
        SelectPathAndUpgrade(UpgradePath.PathB);
    }

    void SelectPathAndUpgrade(UpgradePath selectedPath)
    {
        if (Data == null)
        {
            Debug.LogError($"{name}: TowerData가 연결되지 않아 업그레이드할 수 없습니다.", this);
            return;
        }

        if (level != 1)
        {
            Debug.Log("이미 업그레이드 루트가 선택되었습니다.");
            return;
        }

        if (!CanUpgrade())
        {
            Debug.Log("이미 최대 레벨입니다.");
            return;
        }

        int cost = GetUpgradeCost();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager가 없습니다.");
            return;
        }

        if (!GameManager.Instance.SpendMoney(cost))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }

        path = selectedPath;
        level++;

        ApplyUpgrade();
    }

    public void Upgrade()
    {
        if (Data == null)
        {
            Debug.LogError($"{name}: TowerData가 연결되지 않아 업그레이드할 수 없습니다.", this);
            return;
        }

        if (!CanUpgrade())
        {
            Debug.Log("이미 최대 레벨입니다.");
            return;
        }

        if (path == UpgradePath.None)
        {
            Debug.Log("먼저 업그레이드 루트를 선택해야 합니다.");
            return;
        }

        int cost = GetUpgradeCost();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager가 없습니다.");
            return;
        }

        if (!GameManager.Instance.SpendMoney(cost))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }

        level++;
        ApplyUpgrade();
    }

    void ApplyUpgrade()
    {
        TowerData.CombatStats targetStats = GetStats(path, level);

        if (path == UpgradePath.PathA)
        {
            if (level == 2)
            {
                ChangeTower(
                    pathALv2TowerSprite,
                    targetStats,
                    pathALv2ProjectilePrefab
                );
            }
            else if (level == 3)
            {
                ChangeTower(
                    pathALv3TowerSprite,
                    targetStats,
                    pathALv3ProjectilePrefab
                );
            }
        }
        else if (path == UpgradePath.PathB)
        {
            if (level == 2)
            {
                ChangeTower(
                    pathBLv2TowerSprite,
                    targetStats,
                    pathBLv2ProjectilePrefab
                );
            }
            else if (level == 3)
            {
                ChangeTower(
                    pathBLv3TowerSprite,
                    targetStats,
                    pathBLv3ProjectilePrefab
                );
            }
        }
    }

    void ChangeTower(Sprite newSprite, TowerData.CombatStats newStats, GameObject newProjectilePrefab)
    {
        if (spriteRenderer != null && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }

        if (towerAttack != null)
        {
            towerAttack.ApplyUpgradeStats(
                newStats.damage,
                newStats.attackCooldown,
                newStats.attackRange,
                newProjectilePrefab
            );
        }

        TowerEffectAnimator effectAnimator = GetComponent<TowerEffectAnimator>();
        if (effectAnimator != null)
        {
            effectAnimator.PlayUpgradeEffect();
        }

        Debug.Log(GetTowerName() + " 업그레이드 완료");
    }

    private TowerData.CombatStats GetStats(UpgradePath targetPath, int targetLevel)
    {
        TowerData data = Data;

        if (data == null)
            return default;

        if (targetPath == UpgradePath.PathA)
            return targetLevel == 2 ? data.pathALv2Stats : data.pathALv3Stats;

        if (targetPath == UpgradePath.PathB)
            return targetLevel == 2 ? data.pathBLv2Stats : data.pathBLv3Stats;

        return default;
    }

    // =========================================================
    // 이름 / 설명 텍스트 (TowerData 에셋에서 읽어옵니다)
    // =========================================================
    private TowerData.PathTextInfo GetPathTextInfo(UpgradePath targetPath)
    {
        TowerData data = Data;

        if (data == null)
            return null;

        if (targetPath == UpgradePath.PathA)
            return data.pathA;

        if (targetPath == UpgradePath.PathB)
            return data.pathB;

        return null;
    }

    public string GetTowerName()
    {
        TowerData data = Data;

        if (data == null)
        {
            Debug.LogWarning($"{name}: TowerData가 연결되지 않아 타워 이름을 표시할 수 없습니다.", this);
            return "타워";
        }

        if (path == UpgradePath.None)
            return data.towerName;

        TowerData.PathTextInfo info = GetPathTextInfo(path);

        if (info == null)
            return data.towerName;

        return level >= 3 ? info.lv3Name : info.lv2Name;
    }

    public string GetPathAName()
    {
        TowerData.PathTextInfo info = GetPathTextInfo(UpgradePath.PathA);
        return info != null ? info.routeName : "루트 A";
    }

    public string GetPathBName()
    {
        TowerData.PathTextInfo info = GetPathTextInfo(UpgradePath.PathB);
        return info != null ? info.routeName : "루트 B";
    }

    // Lv.3(최종 업그레이드) 이름은 위 GetTowerName()의 lv3Name과 동일한 값이라
    // 별도로 하드코딩하지 않고 같은 데이터를 그대로 가져다 씁니다.
    public string GetFinalUpgradeName()
    {
        TowerData.PathTextInfo info = GetPathTextInfo(path);
        return info != null ? info.lv3Name : "최종 업그레이드";
    }

    public string GetPathADescription()
    {
        return BuildPathDescription(UpgradePath.PathA);
    }

    public string GetPathBDescription()
    {
        return BuildPathDescription(UpgradePath.PathB);
    }

    private string BuildPathDescription(UpgradePath targetPath)
    {
        TowerData.PathTextInfo info = GetPathTextInfo(targetPath);
        string featureText = info != null ? info.featureText : "";

        TowerData.CombatStats lv2Stats = GetStats(targetPath, 2);

        return $"{featureText}\n\n" +
            $"<align=left>" +
            $"  공격력: <color=white>{towerAttack.damage} → </color><color=#00FF00>{lv2Stats.damage}</color>\n" +
            $"  공격속도(s): <color=white>{towerAttack.attackCooldown} → </color><color=#00FF00>{lv2Stats.attackCooldown}</color>\n" +
            $"  사거리: <color=white>{towerAttack.attackRange} → </color><color=#00FF00>{lv2Stats.attackRange}</color>" +
            $"</align>";
    }

    public string GetFinalUpgradeDescription()
    {
        if (path == UpgradePath.None)
            return "먼저 업그레이드 루트를 선택해야 합니다.";

        TowerData.PathTextInfo info = GetPathTextInfo(path);
        string effectText = info != null ? info.effectText : "";
        TowerData.CombatStats targetStats = GetStats(path, 3);

        return $"최종 단계로 업그레이드합니다.\n" +
           $"효과: {effectText}\n\n" +
           $"<align=left>" +
           $"  공격력: <color=white>{towerAttack.damage} → </color><color=#00FF00>{targetStats.damage}</color>\n" +
           $"  공격속도(s): <color=white>{towerAttack.attackCooldown} → </color><color=#00FF00>{targetStats.attackCooldown}</color>\n" +
           $"  사거리: <color=white>{towerAttack.attackRange} → </color><color=#00FF00>{targetStats.attackRange}</color>" +
           $"</align>";
    }

    public Sprite GetFinalUpgradeSprite()
    {
        if (path == UpgradePath.PathA)
            return pathALv3TowerSprite;

        if (path == UpgradePath.PathB)
            return pathBLv3TowerSprite;

        return null;
    }

    public Sprite GetPathASprite()
    {
        return pathALv2TowerSprite;
    }

    public Sprite GetPathBSprite()
    {
        return pathBLv2TowerSprite;
    }
}
