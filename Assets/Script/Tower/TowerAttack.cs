using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [Header("Tower Settings")]
    public float attackRange = 4f;
    public float attackCooldown = 1f;
    public int damage = 10;

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
        GameObject newArrowPrefab)
    {
        damage = newDamage;
        attackCooldown = newCooldown;
        attackRange = newRange;

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
