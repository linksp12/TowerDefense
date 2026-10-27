using System.Collections.Generic;
using UnityEngine;

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
            tower.attackRange * tower.attackRange;
    }

    public static MonsterHealth Nearest(TowerAttack tower, List<MonsterHealth> exclude = null)
    {
        MonsterHealth result = null;
        float distance = float.PositiveInfinity;
        foreach (var enemy in All)
        {
            if (!InRange(enemy, tower) || (exclude != null && exclude.Contains(enemy))) continue;
            float candidate = ((Vector2)(enemy.transform.position - tower.transform.position)).sqrMagnitude;
            if (candidate >= distance) continue;
            result = enemy;
            distance = candidate;
        }
        return result;
    }
}
