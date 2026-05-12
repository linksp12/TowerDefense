using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 10;

    private Transform target;

    public void SetTarget(Transform newTarget, int newDamage)
    {
        target = newTarget;
        damage = newDamage;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

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
            MonsterHealth monsterHealth = target.GetComponent<MonsterHealth>();

            if (monsterHealth != null)
            {
                monsterHealth.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
