using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolemRootSkill : MonoBehaviour
{
    [Header("스킬 발동")]
    [SerializeField] private float attackDelay = 10f;

    [Header("타워 정지")]
    [SerializeField] private float freezeDuration = 3f;

    [Header("공격 범위")]
    [SerializeField] private float attackRange = 5f;

    [Header("타겟 수")]
    [SerializeField] private int targetCount = 3;

    [Header("덩굴")]
    [SerializeField] private GameObject vineEffectPrefab;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    // 스킬 준비 중인지
    private bool attackScheduled = false;

    // 현재 공격 중인지
    private bool isAttacking = false;

    // 스킬 쿨타임 중인지
    private bool isCooldown = false;

    // 쿨타임 동안 골렘이 한 번이라도 맞았는지
    private bool wasHitDuringCooldown = false;

    private void Awake()
    {
        // Inspector에서 Animator를 넣지 않아도
        // 같은 GameObject의 Animator를 자동으로 찾는다.
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // =========================================================
    // 골렘이 타워에게 피격될 때마다 호출
    // =========================================================
    public void OnDamaged()
    {
        // 공격을 이미 준비 중이라면
        // 새로운 타이머를 또 만들지 않는다.
        if (attackScheduled)
        {
            return;
        }

        // 현재 공격 중이라면
        // 다음 공격을 위한 신호만 남긴다.
        if (isAttacking)
        {
            wasHitDuringCooldown = true;
            return;
        }

        // 스킬 쿨타임 중이라면
        // 공격을 여러 개 쌓지 않고
        // "다음에 한 번 공격해야 한다"는 것만 기억한다.
        if (isCooldown)
        {
            wasHitDuringCooldown = true;
            return;
        }

        // 아직 스킬을 사용할 수 있으면
        // 10초 후 공격 예약
        StartCoroutine(DelayedAttack());
    }

    // =========================================================
    // 피격 후 공격까지 대기
    // =========================================================
    private IEnumerator DelayedAttack()
    {
        attackScheduled = true;

        yield return new WaitForSeconds(attackDelay);

        attackScheduled = false;

        // 골렘이 죽었다면 실행하지 않는다.
        MonsterHealth health = GetComponent<MonsterHealth>();

        if (health != null && health.IsDead)
        {
            yield break;
        }

        StartRootAttack();
    }

    // =========================================================
    // 실제 Root Attack 시작
    // =========================================================
    private void StartRootAttack()
    {
        if (isAttacking)
        {
            return;
        }

        if (animator == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " : Animator를 찾을 수 없습니다."
            );

            return;
        }

        isAttacking = true;

        // 공격 시작과 동시에 쿨타임 시작
        StartCoroutine(RootAttackCooldown());

        // 골렘 공격 애니메이션 시작
        animator.SetTrigger("RootAttack");
    }

    // =========================================================
    // Root Attack 쿨타임
    // =========================================================
    private IEnumerator RootAttackCooldown()
    {
        isCooldown = true;

        // 공격 후 10초 동안 다시 스킬 사용 불가
        yield return new WaitForSeconds(attackDelay);

        isCooldown = false;

        // 쿨타임 동안 골렘이 맞았다면
        // 다음 공격을 다시 예약한다.
        if (wasHitDuringCooldown)
        {
            wasHitDuringCooldown = false;

            StartCoroutine(DelayedAttack());
        }
    }

    // =========================================================
    // Root Attack Animation Event
    //
    // 골렘이 땅을 내려치는 순간 호출
    // =========================================================
    public void SpawnRootAttack()
    {
        if (!isAttacking)
        {
            return;
        }

        // 모든 타워 찾기
        TowerAttack[] allTowers =
            FindObjectsByType<TowerAttack>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        List<TowerAttack> targetTowers =
            new List<TowerAttack>();

        // =====================================================
        // 골렘 주변 타워 검색
        // =====================================================
        foreach (TowerAttack tower in allTowers)
        {
            if (tower == null)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                tower.transform.position
            );

            if (distance <= attackRange)
            {
                targetTowers.Add(tower);
            }
        }

        // =====================================================
        // 가까운 타워 순으로 정렬
        // =====================================================
        targetTowers.Sort((a, b) =>
        {
            float distanceA = Vector3.Distance(
                transform.position,
                a.transform.position
            );

            float distanceB = Vector3.Distance(
                transform.position,
                b.transform.position
            );

            return distanceA.CompareTo(distanceB);
        });

        // =====================================================
        // 최대 3개
        // =====================================================
        int count = Mathf.Min(
            targetCount,
            targetTowers.Count
        );

        // =====================================================
        // 타워 정지 + 덩굴 생성
        // =====================================================
        for (int i = 0; i < count; i++)
        {
            TowerFreeze towerFreeze =
                targetTowers[i].GetComponent<TowerFreeze>();

            if (towerFreeze != null)
            {
                towerFreeze.Freeze(
                    freezeDuration,
                    vineEffectPrefab
                );
            }
        }

        Debug.Log(
            gameObject.name +
            " : 주변 타워 " +
            count +
            "개를 덩굴로 묶었습니다."
        );
    }

    // =========================================================
    // Root Attack Animation 마지막 프레임
    // =========================================================
    public void FinishRootAttack()
    {
        isAttacking = false;
    }
}
