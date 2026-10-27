using System.Collections.Generic;
using UnityEngine;

/// <summary>조준 표시와 실제 시전에서 같은 대상 조건·번개 우선순위를 사용한다.</summary>
internal static class SkillTargetQuery
{
    private static MonsterHealth[] cached;
    private static float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { cached = null; nextScan = 0; }

    public static void Collect(SkillData skill, Vector3 center, float radius,
        List<MonsterHealth> result, bool fresh = false)
    {
        result.Clear();
        if (fresh || cached == null || Time.unscaledTime >= nextScan)
        {
            cached = Object.FindObjectsByType<MonsterHealth>(FindObjectsInactive.Exclude);
            nextScan = Time.unscaledTime + 0.1f;
        }
        foreach (var monster in cached)
        {
            if (monster == null || monster.IsDead || !monster.gameObject.activeInHierarchy) continue;
            if (!float.IsPositiveInfinity(radius) &&
                ((Vector2)(monster.transform.position - center)).sqrMagnitude > radius * radius) continue;
            if (skill.EffectType == SkillEffectType.Ice && monster.GetComponent<MonsterMove>() == null) continue;
            result.Add(monster);
        }
        if (skill.EffectType != SkillEffectType.Lightning) return;
        result.Sort((a, b) => ((Vector2)(a.transform.position - center)).sqrMagnitude.CompareTo(
            ((Vector2)(b.transform.position - center)).sqrMagnitude));
        if (skill.MaxTargets > 0 && result.Count > skill.MaxTargets)
            result.RemoveRange(skill.MaxTargets, result.Count - skill.MaxTargets);
    }
}
