using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed => 8f;
    public int damage = 10;

    [Header("Rotation Settings")]
    public float rotationOffset = 0f;

    [Header("Pierce Settings")]
    public bool canPierce = false;
    public int maxHitCount = 1;

    [Header("Explosion Settings")]
    public bool canExplode = false;
    public float explosionRadius = 1.5f;
    public float splashDamageRate = 0.7f;

    [Header("Slow Settings")]
    public bool canSlow = false;
    [Range(0f, 1f)] public float slowRate = 0.5f;
    public float slowDuration = 2f;

    [Header("DoT Settings")]
    public bool canDot = false;
    public int dotDamage = 5;
    public float dotDuration = 3f;
    public float dotInterval = 1f;

    private Transform target;
    private int currentHitCount = 0;
    private bool canHitStealth = false;

    private HashSet<GameObject> hitMonsters = new HashSet<GameObject>();

    public void SetTarget(
        Transform newTarget,
        int newDamage,
        bool newCanHitStealth)
    {
        target = newTarget;
        damage = newDamage;
        canHitStealth = newCanHitStealth;
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

            monsterHealth.TakeDamage(damage);
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
                    monsterHealth.TakeDamage(splashDamage);
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
