using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [Header("Balance Data")]
    [Tooltip("연결하면 기본 공격 수치와 설치 비용을 이 데이터에서 읽습니다.")]
    public TowerData towerData;

    // 전투 중 변하는 현재 수치입니다. 저장 원본은 TowerData이며 프리팹에는 직렬화하지 않습니다.
    [System.NonSerialized] public float attackRange;
    [System.NonSerialized] public float attackCooldown;
    [System.NonSerialized] public int damage;
    [System.NonSerialized] public TowerData.ProjectileEffectStats projectileEffects;

    [Header("Stealth Detection")]
    public bool canDetectStealth = false;

    [Header("Critical Settings")]
    [Range(0f, 1f)]
    public float criticalChance = 0.1f;

    [Min(1f)]
    public float criticalDamageMultiplier = 2f;

    [Header("Projectile")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private float attackTimer = 0f;

    // 프리팹을 직접 참조하는 UI/건설 코드도 데이터 에셋의 기본값을 사용할 수 있도록 제공합니다.
    public int BuildCost => towerData != null ? towerData.buildCost : 0;
    public int BaseDamage => towerData != null ? towerData.baseStats.damage : damage;
    public float BaseAttackCooldown => towerData != null ? towerData.baseStats.attackCooldown : attackCooldown;
    public float BaseAttackRange => towerData != null ? towerData.baseStats.attackRange : attackRange;

    public string UpgradeRouteSummary
    {
        get
        {
            if (towerData == null)
                return "정보 없음";

            string pathAName = towerData.pathA != null ? towerData.pathA.routeName : "";
            string pathBName = towerData.pathB != null ? towerData.pathB.routeName : "";

            if (string.IsNullOrEmpty(pathAName))
                return pathBName;

            if (string.IsNullOrEmpty(pathBName))
                return pathAName;

            return $"{pathAName} / {pathBName}";
        }
    }

    private void Awake()
    {
        ApplyBaseStatsFromData();
    }

    public void ApplyBaseStatsFromData()
    {
        if (towerData == null)
        {
            Debug.LogError($"{name}: TowerData가 연결되지 않았습니다.", this);
            return;
        }

        damage = towerData.baseStats.damage;
        attackCooldown = towerData.baseStats.attackCooldown;
        attackRange = towerData.baseStats.attackRange;
        projectileEffects = towerData.baseProjectileEffects;
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;

        GameObject target = FindNearestMonster();

        if (target != null &&
            attackTimer >= attackCooldown)
        {
            Attack(target);
            attackTimer = 0f;
        }
    }

    // =========================================================
    // 가장 가까운 살아있는 몬스터 찾기
    // =========================================================
    private GameObject FindNearestMonster()
    {
        GameObject[] monsters =
            GameObject.FindGameObjectsWithTag("Monster");

        GameObject nearestMonster = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject monster in monsters)
        {
            if (monster == null)
                continue;

            // MonsterHealth 가져오기
            MonsterHealth monsterHealth =
                monster.GetComponent<MonsterHealth>();

            // MonsterHealth가 없으면 공격 대상에서 제외
            if (monsterHealth == null)
                continue;

            // 죽은 몬스터는 공격 대상에서 제외
            if (monsterHealth.IsDead)
                continue;

            // 은신 몬스터 처리
            StealthMonster stealthMonster =
                monster.GetComponent<StealthMonster>();

            if (stealthMonster != null &&
                stealthMonster.IsStealthed &&
                !canDetectStealth)
            {
                continue;
            }

            // 거리 계산
            float distance =
                Vector2.Distance(
                    transform.position,
                    monster.transform.position
                );

            // 사거리 내 가장 가까운 몬스터 선택
            if (distance <= attackRange &&
                distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestMonster = monster;
            }
        }

        return nearestMonster;
    }

    // =========================================================
    // 공격
    // =========================================================
    private void Attack(GameObject target)
    {
        if (target == null)
            return;

        // 공격 직전 다시 사망 여부 확인
        MonsterHealth targetHealth =
            target.GetComponent<MonsterHealth>();

        if (targetHealth == null)
            return;

        if (targetHealth.IsDead)
            return;

        // 화살 프리팹 확인
        if (arrowPrefab == null)
        {
            Debug.LogWarning(
                "Arrow Prefab이 연결되지 않았습니다."
            );

            return;
        }

        Vector3 spawnPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;

        // 화살 생성
        GameObject arrow =
            Instantiate(
                arrowPrefab,
                spawnPosition,
                Quaternion.identity
            );

        // ArrowProjectile 가져오기
        ArrowProjectile projectile =
            arrow.GetComponent<ArrowProjectile>();

        if (projectile != null)
        {
            projectile.SetTarget(
                target.transform,
                damage,
                canDetectStealth,
                criticalChance,
                criticalDamageMultiplier
            );
            projectile.ApplyEffectStats(projectileEffects);
        }
        else
        {
            Debug.LogWarning(
                "Arrow Prefab에 ArrowProjectile이 없습니다."
            );
        }
    }

    // =========================================================
    // 사거리 표시
    // =========================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }

    // =========================================================
    // 업그레이드 능력치 적용
    // =========================================================
    public void ApplyUpgradeStats(
        int newDamage,
        float newCooldown,
        float newRange,
        GameObject newArrowPrefab,
        TowerData.ProjectileEffectStats newProjectileEffects)
    {
        damage = newDamage;
        attackCooldown = newCooldown;
        attackRange = newRange;
        projectileEffects = newProjectileEffects;

        if (newArrowPrefab != null)
        {
            arrowPrefab = newArrowPrefab;
        }

        Debug.Log(
            "타워 능력치 변경 완료 / 공격력: " +
            damage +
            " / 쿨타임: " +
            attackCooldown +
            " / 사거리: " +
            attackRange
        );
    }
}
