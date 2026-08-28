using UnityEngine;

public class TowerUpgrade : MonoBehaviour
{
    public enum TowerType
    {
        Archer,
        Cannon,
        Magic
    }

    public enum UpgradePath
    {
        None,
        PathA,
        PathB
    }

    [Header("타워 종류")]
    public TowerType towerType = TowerType.Archer;

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

    [Header("업그레이드 비용")]
    public int level2Cost = 150;
    public int level3Cost = 300;

    [Header("판매 설정")]
    [Range(0f, 1f)]
    public float sellRate = 0.7f;

    [Header("Path A Lv.2 능력치")]
    public int pathALv2Damage = 15;
    public float pathALv2Cooldown = 0.7f;
    public float pathALv2Range = 4.2f;

    [Header("Path A Lv.3 능력치")]
    public int pathALv3Damage = 20;
    public float pathALv3Cooldown = 0.45f;
    public float pathALv3Range = 4.5f;

    [Header("Path B Lv.2 능력치")]
    public int pathBLv2Damage = 28;
    public float pathBLv2Cooldown = 1.0f;
    public float pathBLv2Range = 4.8f;

    [Header("Path B Lv.3 능력치")]
    public int pathBLv3Damage = 55;
    public float pathBLv3Cooldown = 1.3f;
    public float pathBLv3Range = 5.5f;

    private SpriteRenderer spriteRenderer;
    private TowerAttack towerAttack;

    private BuildPoint ownerBuildPoint;

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
        if (level == 1) return level2Cost;
        if (level == 2) return level3Cost;
        return 0;
    }

    public int GetBaseCost()
    {
        if (towerType == TowerType.Archer) return 50;
        if (towerType == TowerType.Cannon) return 100;
        if (towerType == TowerType.Magic) return 150;

        return 50;
    }

    public int GetTotalUsedCost()
    {
        int totalCost = GetBaseCost();

        if (level >= 2)
            totalCost += level2Cost;

        if (level >= 3)
            totalCost += level3Cost;

        return totalCost;
    }

    public int GetSellPrice()
    {
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
        if (path == UpgradePath.PathA)
        {
            if (level == 2)
            {
                ChangeTower(
                    pathALv2TowerSprite,
                    pathALv2Damage,
                    pathALv2Cooldown,
                    pathALv2Range,
                    pathALv2ProjectilePrefab
                );
            }
            else if (level == 3)
            {
                ChangeTower(
                    pathALv3TowerSprite,
                    pathALv3Damage,
                    pathALv3Cooldown,
                    pathALv3Range,
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
                    pathBLv2Damage,
                    pathBLv2Cooldown,
                    pathBLv2Range,
                    pathBLv2ProjectilePrefab
                );
            }
            else if (level == 3)
            {
                ChangeTower(
                    pathBLv3TowerSprite,
                    pathBLv3Damage,
                    pathBLv3Cooldown,
                    pathBLv3Range,
                    pathBLv3ProjectilePrefab
                );
            }
        }
    }

    void ChangeTower(Sprite newSprite, int newDamage, float newCooldown, float newRange, GameObject newProjectilePrefab)
    {
        if (spriteRenderer != null && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }

        if (towerAttack != null)
        {
            towerAttack.ApplyUpgradeStats(newDamage, newCooldown, newRange, newProjectilePrefab);
        }

        TowerEffectAnimator effectAnimator = GetComponent<TowerEffectAnimator>();
        if (effectAnimator != null)
        {
            effectAnimator.PlayUpgradeEffect();
        }

        Debug.Log(GetTowerName() + " 업그레이드 완료");
    }

    public string GetTowerName()
    {
        if (towerType == TowerType.Archer)
            return GetArcherTowerName();

        if (towerType == TowerType.Cannon)
            return GetCannonTowerName();

        if (towerType == TowerType.Magic)
            return GetMagicTowerName();

        return "타워";
    }

    string GetArcherTowerName()
    {
        if (path == UpgradePath.None)
            return "기본 화살 타워";

        if (path == UpgradePath.PathA)
        {
            if (level == 2) return "연사 화살 타워";
            if (level == 3) return "폭풍 화살 타워";
        }

        if (path == UpgradePath.PathB)
        {
            if (level == 2) return "강화 화살 타워";
            if (level == 3) return "관통 화살 타워";
        }

        return "기본 화살 타워";
    }

    string GetCannonTowerName()
    {
        if (path == UpgradePath.None)
            return "기본 캐논 타워";

        if (path == UpgradePath.PathA)
        {
            if (level == 2) return "폭발 캐논";
            if (level == 3) return "대폭발 캐논";
        }

        if (path == UpgradePath.PathB)
        {
            if (level == 2) return "중포 타워";
            if (level == 3) return "공성포 타워";
        }

        return "기본 캐논 타워";
    }

    string GetMagicTowerName()
    {
        if (path == UpgradePath.None)
            return "기본 마법 타워";

        if (path == UpgradePath.PathA)
        {
            if (level == 2) return "강화 마법화살 타워";
            if (level == 3) return "연속 마법화살 타워";
        }

        if (path == UpgradePath.PathB)
        {
            if (level == 2) return "화염 마법 타워";
            if (level == 3) return "지옥불 마법 타워";
        }

        return "기본 마법 타워";
    }

    public string GetPathAName()
    {
        if (towerType == TowerType.Archer) return "연사 루트";
        if (towerType == TowerType.Cannon) return "폭발 루트";
        if (towerType == TowerType.Magic) return "마법화살 루트";

        return "루트 A";
    }

    public string GetPathBName()
    {
        if (towerType == TowerType.Archer) return "관통 루트";
        if (towerType == TowerType.Cannon) return "공성 루트";
        if (towerType == TowerType.Magic) return "화염 루트";

        return "루트 B";
    }

    public string GetFinalUpgradeName()
    {
        if (path == UpgradePath.PathA)
        {
            if (towerType == TowerType.Archer) return "폭풍 화살 타워";
            if (towerType == TowerType.Cannon) return "대폭발 캐논";
            if (towerType == TowerType.Magic) return "연속 마법화살 타워";
        }

        if (path == UpgradePath.PathB)
        {
            if (towerType == TowerType.Archer) return "관통 화살 타워";
            if (towerType == TowerType.Cannon) return "공성포 타워";
            if (towerType == TowerType.Magic) return "지옥불 마법 타워";
        }

        return "최종 업그레이드";
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
