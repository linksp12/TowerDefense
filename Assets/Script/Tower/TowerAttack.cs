using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [Header("Tower Settings")]
    public float attackRange = 4f;
    public float attackCooldown = 1f;
    public int damage = 10;

    [Header("Projectile")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private float attackTimer = 0f;

    void Update()
    {
        attackTimer += Time.deltaTime;

        GameObject target = FindNearestMonster();

        if (target != null && attackTimer >= attackCooldown)
        {
            Attack(target);
            attackTimer = 0f;
        }
    }

    GameObject FindNearestMonster()
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");

        GameObject nearestMonster = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject monster in monsters)
        {
            float distance = Vector2.Distance(transform.position, monster.transform.position);

            if (distance <= attackRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestMonster = monster;
            }
        }

        return nearestMonster;
    }

    void Attack(GameObject target)
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("Arrow Prefab이 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        GameObject arrow = Instantiate(arrowPrefab, spawnPosition, Quaternion.identity);

        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();

        if (projectile != null)
        {
            projectile.SetTarget(target.transform, damage);
            Debug.Log("화살 발사: " + target.name);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
