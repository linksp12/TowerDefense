using System.Collections;
using UnityEngine;

/// <summary>
/// 얼음 마녀 보스
/// - 얼음 결계: 일정 간격으로 지팡이를 들어 주변 몬스터(자신 제외)에게 잠깐 무적 얼음 보호막을 씌웁니다.
/// - 분신: 체력이 일정 비율 아래로 떨어지면 한 번, 길 앞쪽으로 순간이동하며 분신을 남깁니다.
/// </summary>
public class IceWitchBoss : MonoBehaviour
{
    [Header("얼음 결계")]
    [SerializeField, Min(0f)] private float firstDelay = 3f;
    [SerializeField, Min(1f)] private float interval = 10f;
    [SerializeField, Min(0.5f)] private float barrierRadius = 3.5f;
    [SerializeField, Min(0.5f)] private float barrierDuration = 3f;
    [SerializeField] private Sprite barrierShell;

    [Header("분신")]
    [SerializeField, Range(0.05f, 0.95f)] private float cloneHpRatio = 0.5f;
    [SerializeField] private GameObject clonePrefab;
    [SerializeField, Min(0)] private int cloneCount = 2;
    [SerializeField, Min(0f)] private float blinkDistance = 2f;

    [Header("Animator State")]
    [SerializeField] private string castState = "ice_witch_cast";
    [SerializeField] private string walkState = "ice_witch_walk";
    [SerializeField, Min(0.1f)] private float castTime = 0.8f;

    private MonsterHealth health;
    private MonsterMove move;
    private Animator animator;
    private bool cloned;

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
        move = GetComponent<MonsterMove>();
        animator = GetComponent<Animator>();
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

    private void Start()
    {
        StartCoroutine(BarrierLoop());
    }

    private IEnumerator BarrierLoop()
    {
        yield return new WaitForSeconds(firstDelay);

        while (health != null && !health.IsDead)
        {
            yield return CastBarrier();
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator CastBarrier()
    {
        if (move != null)
            move.enabled = false;
        Play(castState);

        yield return new WaitForSeconds(castTime * 0.5f);

        if (health == null || health.IsDead)
            yield break;

        CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, transform.position, false, 0f, false, true);

        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, barrierRadius))
        {
            if (col.gameObject == gameObject)
                continue;

            if (col.TryGetComponent(out MonsterHealth other) && !other.IsDead)
            {
                MonsterWard.For(other.gameObject).ApplyInvulnerable(barrierDuration, barrierShell);
                CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, other.transform.position, false, 0f, false, true);
            }
        }

        yield return new WaitForSeconds(castTime * 0.5f);

        if (health != null && !health.IsDead)
        {
            if (move != null)
                move.enabled = true;
            Play(walkState);
        }
    }

    private void OnDamaged(MonsterHealth monster, int damage)
    {
        if (cloned || monster.CurrentHp > monster.maxHp * cloneHpRatio)
            return;

        cloned = true;

        Vector3 from = transform.position;
        int fromIndex = move != null ? move.CurrentWaypointIndex : 0;
        if (move != null)
            move.AdvanceAlongPath(blinkDistance);

        CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, from, false, 0f, false, true);
        CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, transform.position, false, 0f, false, true);
        MonsterAbilityUtil.Shake(0.35f);

        // 분신은 원래 자리 근처에 남아 타워의 공격을 끌어간다.
        if (cloneCount > 0 && move != null)
        {
            foreach (GameObject clone in MonsterAbilityUtil.SpawnOnPath(clonePrefab, move, from, cloneCount, 0.9f))
            {
                if (clone.TryGetComponent(out MonsterMove cloneMove))
                    cloneMove.ContinueFrom(move.waypoints, fromIndex);
            }
        }
    }

    private void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state))
            animator.Play(state, 0, 0f);
    }
}
