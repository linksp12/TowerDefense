using UnityEngine;

/// <summary>
/// 임프: 공격받으면 길 앞쪽으로 짧게 순간이동합니다. (쿨타임 동안은 다시 쓰지 않음)
/// </summary>
public class ImpBlink : MonoBehaviour
{
    [SerializeField, Min(0.2f)] private float distance = 1.6f;
    [SerializeField, Min(0.5f)] private float cooldown = 4f;

    private MonsterHealth health;
    private MonsterMove move;
    private float readyTime;

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
        move = GetComponent<MonsterMove>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Damaged -= OnDamaged;
    }

    private void OnDamaged(MonsterHealth monster, int damage)
    {
        if (Time.time < readyTime || move == null || !move.enabled)
            return;

        readyTime = Time.time + cooldown;

        Vector3 from = transform.position;
        move.AdvanceAlongPath(distance);

        // 사라진 자리와 나타난 자리에 불꽃
        CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, from, false, 0f, true, false);
        CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, transform.position, false, 0f, true, false);
    }
}
