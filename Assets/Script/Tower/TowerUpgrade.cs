using UnityEngine;

public class TowerUpgrade : MonoBehaviour
{
    public enum UpgradePath
    {
        None,
        Rapid,
        Pierce
    }

    [Header("현재 상태")]
    public int level = 1;
    public int maxLevel = 3;
    public UpgradePath path = UpgradePath.None;

    [Header("타워 이미지")]
    public Sprite basicTowerSprite;
    public Sprite rapidLv2TowerSprite;
    public Sprite rapidLv3TowerSprite;
    public Sprite pierceLv2TowerSprite;
    public Sprite pierceLv3TowerSprite;

    [Header("화살 프리팹")]
    public GameObject basicArrowPrefab;
    public GameObject rapidLv2ArrowPrefab;
    public GameObject rapidLv3ArrowPrefab;
    public GameObject pierceLv2ArrowPrefab;
    public GameObject pierceLv3ArrowPrefab;

    [Header("업그레이드 비용")]
    public int level2Cost = 150;
    public int level3Cost = 300;

    private SpriteRenderer spriteRenderer;
    private TowerAttack towerAttack;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        towerAttack = GetComponent<TowerAttack>();
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

    public void SelectRapidPath()
    {
        if (level != 1)
        {
            Debug.Log("이미 업그레이드 루트가 선택되었습니다.");
            return;
        }

        path = UpgradePath.Rapid;
        Upgrade();
    }

    public void SelectPiercePath()
    {
        if (level != 1)
        {
            Debug.Log("이미 업그레이드 루트가 선택되었습니다.");
            return;
        }

        path = UpgradePath.Pierce;
        Upgrade();
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
        if (path == UpgradePath.Rapid)
        {
            if (level == 2)
            {
                ChangeTower(rapidLv2TowerSprite, 15, 0.7f, 4.2f, rapidLv2ArrowPrefab);
            }
            else if (level == 3)
            {
                ChangeTower(rapidLv3TowerSprite, 20, 0.45f, 4.5f, rapidLv3ArrowPrefab);
            }
        }
        else if (path == UpgradePath.Pierce)
        {
            if (level == 2)
            {
                ChangeTower(pierceLv2TowerSprite, 25, 1.2f, 4.8f, pierceLv2ArrowPrefab);
            }
            else if (level == 3)
            {
                ChangeTower(pierceLv3TowerSprite, 40, 1.5f, 5.5f, pierceLv3ArrowPrefab);
            }
        }
    }

    void ChangeTower(Sprite newSprite, int newDamage, float newCooldown, float newRange, GameObject newArrowPrefab)
    {
        if (spriteRenderer != null && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }

        if (towerAttack != null)
        {
            towerAttack.ApplyUpgradeStats(newDamage, newCooldown, newRange, newArrowPrefab);
        }

        Debug.Log(GetTowerName() + " 업그레이드 완료");
    }

    public string GetTowerName()
    {
        if (path == UpgradePath.None)
        {
            return "기본 화살 타워";
        }

        if (path == UpgradePath.Rapid)
        {
            if (level == 2) return "연사 화살 타워";
            if (level == 3) return "폭풍 화살 타워";
        }

        if (path == UpgradePath.Pierce)
        {
            if (level == 2) return "강화 화살 타워";
            if (level == 3) return "관통 화살 타워";
        }

        return "기본 화살 타워";
    }
}
