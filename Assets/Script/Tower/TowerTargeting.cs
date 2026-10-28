using System.Collections.Generic;
using UnityEngine;

/// <summary>타워가 사거리 안에서 먼저 노릴 적.</summary>
public enum TargetPriority
{
    First,      // 선두: 기지에 가장 가까운 적
    Last,       // 후미: 가장 뒤에 있는 적
    Strongest,  // 강한 적: 보스·체력이 가장 많은 적
    Nearest     // 가까운 적: 타워에서 가장 가까운 적
}

/// <summary>새 공격들이 공유하는 짧은 주기의 적 목록. 매 타워/탄환마다 씬을 검색하지 않는다.</summary>
internal static class TowerTargeting
{
    private static MonsterHealth[] cached = System.Array.Empty<MonsterHealth>();
    private static float refreshAt = -1;

    public static IReadOnlyList<MonsterHealth> All
    {
        get
        {
            if (Time.time >= refreshAt)
            {
                cached = Object.FindObjectsByType<MonsterHealth>(FindObjectsInactive.Exclude);
                refreshAt = Time.time + 0.1f;
            }
            return cached;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { cached = System.Array.Empty<MonsterHealth>(); refreshAt = -1; }

    public static bool CanHit(MonsterHealth enemy, bool detectStealth)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead) return false;
        var stealth = enemy.GetComponent<StealthMonster>();
        return detectStealth || stealth == null || !stealth.IsStealthed;
    }

    public static bool InRange(MonsterHealth enemy, TowerAttack tower)
    {
        return CanHit(enemy, tower.canDetectStealth) &&
            ((Vector2)(enemy.transform.position - tower.transform.position)).sqrMagnitude <=
            tower.EffectiveRange * tower.EffectiveRange;
    }

    public static MonsterHealth Nearest(TowerAttack tower, List<MonsterHealth> exclude = null) =>
        Select(tower, TargetPriority.Nearest, exclude);

    // 타워에 정해진 우선순위로 사거리 안의 적을 고른다.
    public static MonsterHealth Select(TowerAttack tower, List<MonsterHealth> exclude = null) =>
        Select(tower, tower.targetPriority, exclude);

    public static MonsterHealth Select(TowerAttack tower, TargetPriority priority, List<MonsterHealth> exclude,
        System.Predicate<MonsterHealth> filter = null)
    {
        MonsterHealth result = null;
        float best = float.PositiveInfinity;
        foreach (var enemy in All)
        {
            if (!InRange(enemy, tower) || (exclude != null && exclude.Contains(enemy))) continue;
            if (filter != null && !filter(enemy)) continue;
            float score = Score(enemy, tower, priority);
            if (score >= best) continue;
            result = enemy;
            best = score;
        }
        return result;
    }

    // 작을수록 먼저 노린다.
    private static float Score(MonsterHealth enemy, TowerAttack tower, TargetPriority priority)
    {
        float distance = ((Vector2)(enemy.transform.position - tower.transform.position)).sqrMagnitude;
        switch (priority)
        {
            case TargetPriority.First:
                return enemy.Mover != null ? enemy.Mover.RemainingPathDistance : distance;
            case TargetPriority.Last:
                return enemy.Mover != null ? -enemy.Mover.RemainingPathDistance : -distance;
            case TargetPriority.Strongest:
                // 보스를 가장 먼저, 같은 무리에서는 체력이 많은 적. 같으면 가까운 쪽.
                return -(enemy.CurrentHp + (enemy.isBoss ? 1000000f : 0f)) + distance * 0.001f;
            default:
                return distance;
        }
    }

    public static string Label(TargetPriority priority) => priority switch
    {
        TargetPriority.First => "선두",
        TargetPriority.Last => "후미",
        TargetPriority.Strongest => "강한 적",
        _ => "가까운 적"
    };
}
