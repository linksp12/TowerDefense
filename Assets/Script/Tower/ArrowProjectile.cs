using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 10;

    [Header("Pierce Settings")]
    public bool canPierce = false;
    public int maxHitCount = 1;

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
        transform.rotation = Quaternion.Euler(0, 0, angle);

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

            Debug.Log("화살 피격: " + monster.name + " / 피격 수: " + currentHitCount);
        }

        if (!canPierce || currentHitCount >= maxHitCount)
        {
            Destroy(gameObject);
            return;
        }

        FindNextTarget();
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
