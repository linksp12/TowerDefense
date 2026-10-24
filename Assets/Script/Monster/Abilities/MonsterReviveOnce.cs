using System.Collections;
using UnityEngine;

/// <summary>
/// 해골 병사: 처음 쓰러지면 뼈 무더기가 되었다가 잠시 후 체력 일부로 다시 일어납니다.
/// 쓰러진 동안은 공격 대상이 아니며 웨이브 남은 수에서도 빠지지 않습니다.
/// </summary>
public class MonsterReviveOnce : MonoBehaviour
{
    [Header("부활")]
    [SerializeField, Min(0.1f)] private float downDuration = 1.6f;
    [SerializeField, Range(0.05f, 1f)] private float reviveHpRatio = 0.5f;

    [Header("Animator State")]
    [SerializeField] private string downState = "skeleton_down";
    [SerializeField] private string riseState = "skeleton_rise";
    [SerializeField] private string walkState = "skeleton_walk";
    [SerializeField, Min(0f)] private float riseTime = 0.4f;

    private bool used;
    private MonsterHealth health;
    private Animator animator;

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
        animator = GetComponent<Animator>();
    }

    /// <summary>MonsterHealth.Die 직전에 호출. true면 이번 죽음을 쓰러짐으로 바꿉니다.</summary>
    public bool TryInterceptDeath()
    {
        if (used || health == null)
            return false;

        used = true;
        StartCoroutine(ReviveRoutine());
        return true;
    }

    private IEnumerator ReviveRoutine()
    {
        health.SetDowned(true);
        Play(downState);

        yield return new WaitForSeconds(downDuration);

        Play(riseState);
        CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, transform.position, false, 0f, true, false);

        yield return new WaitForSeconds(riseTime);

        health.RestoreHpRatio(reviveHpRatio);
        health.SetDowned(false);
        Play(walkState);
    }

    private void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state))
            animator.Play(state, 0, 0f);
    }
}
