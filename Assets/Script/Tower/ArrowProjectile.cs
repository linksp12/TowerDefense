using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed = 6f;
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

    private HashSet<GameObject> hitMonsters = new HashSet<GameObject>();

    public void SetTarget(Transform newTarget, int newDamage)
    {
        target = newTarget;
        damage = newDamage;
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
        if (hitMonsters.Contains(monster))
        {
            FindNextTarget();
            return;
        }

        MonsterHealth monsterHealth = monster.GetComponent<MonsterHealth>();

        if (monsterHealth != null)
        {
            monsterHealth.TakeDamage(damage);
            hitMonsters.Add(monster);
            currentHitCount++;

            Debug.Log("발사체 피격: " + monster.name + " / 피격 수: " + currentHitCount);
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
                Debug.Log("슬로우 적용: " + monster.name);
            }
        }

        if (canDot)
        {
            MonsterStatus monsterStatus = monster.GetComponent<MonsterStatus>();

            if (monsterStatus != null)
            {
                monsterStatus.ApplyDot(dotDamage, dotDuration, dotInterval);
                Debug.Log("지속 피해 적용: " + monster.name);
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

            float distance = Vector2.Distance(transform.position, monster.transform.position);

            if (distance <= explosionRadius)
            {
                MonsterHealth monsterHealth = monster.GetComponent<MonsterHealth>();

                if (monsterHealth != null)
                {
                    int splashDamage = Mathf.RoundToInt(damage * splashDamageRate);
                    monsterHealth.TakeDamage(splashDamage);

                    Debug.Log("폭발 피해: " + monster.name + " / 데미지: " + splashDamage);
                }

                ApplySpecialEffect(monster);
            }
        }

        Debug.Log("폭발 발생 / 반경: " + explosionRadius);
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
}
