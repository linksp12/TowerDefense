using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed => 8f;
    private int damage = 10;

    [Header("Rotation Settings")]
    public float rotationOffset = 0f;

    // 투사체 특수효과는 프리팹 Inspector가 아니라 TowerData에서 발사 시점에 적용됩니다.
    private bool canPierce;
    private int maxHitCount = 1;
    private bool canExplode;
    private float explosionRadius;
    private float splashDamageRate;
    private bool canSlow;
    private float slowRate;
    private float slowDuration;
    private bool canDot;
    private int dotDamage;
    private float dotDuration;
    private float dotInterval;

    private Transform target;
    private int currentHitCount = 0;
    private bool canHitStealth = false;
    private float criticalChance = 0.1f;
    private float criticalDamageMultiplier = 2f;

    private HashSet<GameObject> hitMonsters = new HashSet<GameObject>();

    public void SetTarget(
        Transform newTarget,
        int newDamage,
        bool newCanHitStealth,
        float newCriticalChance,
        float newCriticalDamageMultiplier)
    {
        target = newTarget;
        damage = newDamage;
        canHitStealth = newCanHitStealth;
        criticalChance = Mathf.Clamp01(newCriticalChance);
        criticalDamageMultiplier = Mathf.Max(1f, newCriticalDamageMultiplier);
    }

    public void ApplyEffectStats(TowerData.ProjectileEffectStats effectStats)
    {
        canPierce = effectStats.canPierce;
        maxHitCount = Mathf.Max(1, effectStats.maxHitCount);

        canExplode = effectStats.canExplode;
        explosionRadius = Mathf.Max(0f, effectStats.explosionRadius);
        splashDamageRate = Mathf.Clamp01(effectStats.splashDamageRate);

        canSlow = effectStats.canSlow;
        slowRate = Mathf.Clamp01(effectStats.slowRate);
        slowDuration = Mathf.Max(0f, effectStats.slowDuration);

        canDot = effectStats.canDot;
        dotDamage = Mathf.Max(0, effectStats.dotDamage);
        dotDuration = Mathf.Max(0f, effectStats.dotDuration);
        dotInterval = Mathf.Max(0f, effectStats.dotInterval);
    }

    void Update()
    {
        if (target == null)
        {
            if (canPierce)
            {
                FindNextTarget();
            }

            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        MoveToTarget();
    }

    void MoveToTarget()
    {
        Vector2 direction = target.position - transform.position;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            HitTarget(target.gameObject);
        }
    }

    void HitTarget(GameObject monster)
    {
        if (!CanHitMonster(monster))
        {
            Destroy(gameObject);
            return;
        }

        if (hitMonsters.Contains(monster))
        {
            FindNextTarget();
            return;
        }

        MonsterHealth monsterHealth = monster.GetComponent<MonsterHealth>();

        if (monsterHealth != null)
        {
            StealthMonster stealthMonster = monster.GetComponent<StealthMonster>();

            if (stealthMonster != null && canHitStealth)
            {
                stealthMonster.Reveal();
            }

            monsterHealth.TakeDamage(
                damage,
                true,
                criticalChance,
                criticalDamageMultiplier
            );
            hitMonsters.Add(monster);
            currentHitCount++;
        }

        ApplySpecialEffect(monster);

        if (canExplode)
        {
            Explode(monster);
            Destroy(gameObject);
            return;
        }

        if (!canPierce || currentHitCount >= maxHitCount)
        {
            Destroy(gameObject);
            return;
        }

        FindNextTarget();
    }

    void ApplySpecialEffect(GameObject monster)
    {
        if (canSlow)
        {
            MonsterMove monsterMove = monster.GetComponent<MonsterMove>();

            if (monsterMove != null)
            {
                monsterMove.ApplySlow(slowRate, slowDuration);
            }
        }

        if (canDot)
        {
            MonsterStatus monsterStatus = monster.GetComponent<MonsterStatus>();

            if (monsterStatus != null)
            {
                monsterStatus.ApplyDot(dotDamage, dotDuration, dotInterval);
            }
        }
    }

    void Explode(GameObject mainTarget)
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");

        foreach (GameObject monster in monsters)
        {
            if (monster == null) continue;
            if (monster == mainTarget) continue;
            if (!CanHitMonster(monster)) continue;

            float distance = Vector2.Distance(transform.position, monster.transform.position);

            if (distance <= explosionRadius)
            {
                MonsterHealth monsterHealth = monster.GetComponent<MonsterHealth>();

                if (monsterHealth != null)
                {
                    if (canHitStealth)
                    {
                        StealthMonster stealthMonster = monster.GetComponent<StealthMonster>();
                        stealthMonster?.Reveal();
                    }

                    int splashDamage = Mathf.RoundToInt(damage * splashDamageRate);
                    monsterHealth.TakeDamage(
                        splashDamage,
                        true,
                        criticalChance,
                        criticalDamageMultiplier
                    );
                }

                ApplySpecialEffect(monster);
            }
        }
    }

    void FindNextTarget()
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");

        GameObject nearestMonster = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject monster in monsters)
        {
            if (monster == null) continue;
            if (hitMonsters.Contains(monster)) continue;
            if (!CanHitMonster(monster)) continue;

            float distance = Vector2.Distance(transform.position, monster.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestMonster = monster;
            }
        }

        if (nearestMonster != null)
        {
            target = nearestMonster.transform;
        }
        else
        {
            target = null;
        }
    }

    bool CanHitMonster(GameObject monster)
    {
        if (monster == null)
            return false;

        StealthMonster stealthMonster = monster.GetComponent<StealthMonster>();

        if (stealthMonster == null || !stealthMonster.IsStealthed)
            return true;

        return canHitStealth;
    }
}
