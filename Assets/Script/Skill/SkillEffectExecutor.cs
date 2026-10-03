using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Owns combat effects; SkillManager keeps skill lookup, cooldowns, audio and notifications.
internal sealed class SkillEffectExecutor
{
    private readonly MonoBehaviour owner;

    public SkillEffectExecutor(MonoBehaviour owner) => this.owner = owner;

    public bool TryExecute(SkillData skill, Vector3 position, float radius)
    {
        CastStats stats = new CastStats(skill);
        switch (skill.EffectType)
        {
            case SkillEffectType.Fire:
                ApplyFire(skill, position, radius, stats);
                return true;
            case SkillEffectType.Ice:
                ApplyIce(skill, position, radius, stats);
                return true;
            case SkillEffectType.Lightning:
                ApplyLightning(skill, position, radius, stats);
                return true;
            default:
                Debug.LogError($"지원하지 않는 스킬 효과입니다: {skill.EffectType}", skill);
                return false;
        }
    }

    // Resolve permanent bonuses once per cast, including its delayed damage ticks.
    private readonly struct CastStats
    {
        public readonly int Damage;
        public readonly int PeriodicDamage;
        public readonly float Duration;

        public CastStats(SkillData skill)
        {
            Damage = ResearchStatResolver.GetSkillDamage(skill);
            PeriodicDamage = ResearchStatResolver.GetSkillPeriodicDamage(skill);
            Duration = ResearchStatResolver.GetSkillDuration(skill);
        }
    }
    private void ApplyFire(SkillData skill, Vector3 castPosition, float radius, CastStats stats)
    {
        float duration = stats.Duration;
        MonsterHealth[] monsters = UnityEngine.Object.FindObjectsByType<MonsterHealth>(
            FindObjectsInactive.Exclude);

        foreach (MonsterHealth monster in monsters)
        {
            if (!IsValidTarget(monster, castPosition, radius))
                continue;

            monster.TakeDamage(stats.Damage, false);
            if (skill.PeriodicDamage > 0 && duration > 0f)
                owner.StartCoroutine(ApplyPeriodicDamage(monster, skill, stats));
        }

        CreateEffect(skill, castPosition, Mathf.Max(1f, duration), skill.EffectScale);
    }

    private static IEnumerator ApplyPeriodicDamage(MonsterHealth monster, SkillData skill, CastStats stats)
    {
        float elapsed = 0f;
        float interval = Mathf.Max(0.05f, skill.PeriodicInterval);
        float duration = stats.Duration;

        WaitForSeconds wait = new WaitForSeconds(interval);
        while (elapsed < duration)
        {
            yield return wait;
            if (monster == null || monster.IsDead)
                yield break;

            monster.TakeDamage(stats.PeriodicDamage, false);
            elapsed += interval;
        }
    }

    private void ApplyIce(SkillData skill, Vector3 castPosition, float radius, CastStats stats)
    {
        float duration = stats.Duration;
        MonsterMove[] monsters = UnityEngine.Object.FindObjectsByType<MonsterMove>(
            FindObjectsInactive.Exclude);

        foreach (MonsterMove monster in monsters)
        {
            if (monster == null)
                continue;

            MonsterHealth health = monster.GetComponent<MonsterHealth>();
            if (!IsValidTarget(health, castPosition, radius))
                continue;

            monster.Freeze(duration);
            CreateAttachedEffect(skill, monster.transform, Mathf.Max(1f, duration));
        }
    }

    private void ApplyLightning(SkillData skill, Vector3 castPosition, float radius, CastStats stats)
    {
        MonsterHealth[] allMonsters = UnityEngine.Object.FindObjectsByType<MonsterHealth>(
            FindObjectsInactive.Exclude);
        List<MonsterHealth> targets = new List<MonsterHealth>();

        foreach (MonsterHealth monster in allMonsters)
        {
            if (IsValidTarget(monster, castPosition, radius))
                targets.Add(monster);
        }

        targets.Sort((left, right) =>
            ((Vector2)(left.transform.position - castPosition)).sqrMagnitude.CompareTo(
                ((Vector2)(right.transform.position - castPosition)).sqrMagnitude));

        int targetLimit = skill.MaxTargets > 0 ? skill.MaxTargets : targets.Count;
        int hitCount = Mathf.Min(targetLimit, targets.Count);
        for (int index = 0; index < hitCount; index++)
        {
            MonsterHealth target = targets[index];
            target.TakeDamage(stats.Damage, false);
            CreateEffect(skill, target.transform.position, 1f, skill.EffectScale);
        }
    }

    private static bool IsValidTarget(
        MonsterHealth monster,
        Vector3 center,
        float radius)
    {
        if (monster == null || monster.IsDead)
            return false;

        return float.IsPositiveInfinity(radius)
            || ((Vector2)(monster.transform.position - center)).sqrMagnitude <= radius * radius;
    }

    private static void ConfigureEffectRenderer(GameObject effect)
    {
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        if (renderer == null)
            return;

        renderer.sortingLayerID = SortingLayer.NameToID("Effects");
        renderer.sortingOrder = 100;
    }

    private static void CreateAttachedEffect(
        SkillData skill,
        Transform parent,
        float destroyTime)
    {
        if (skill.EffectPrefab == null)
            return;

        GameObject effect = UnityEngine.Object.Instantiate(skill.EffectPrefab, parent);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
        effect.transform.localScale *= skill.EffectScale;
        ConfigureEffectRenderer(effect);
        UnityEngine.Object.Destroy(effect, destroyTime);
    }

    private static void CreateEffect(
        SkillData skill,
        Vector3 position,
        float destroyTime,
        float scale)
    {
        if (skill.EffectPrefab == null)
            return;

        GameObject effect = UnityEngine.Object.Instantiate(skill.EffectPrefab, position, Quaternion.identity);
        effect.transform.localScale *= scale;
        ConfigureEffectRenderer(effect);
        UnityEngine.Object.Destroy(effect, destroyTime);
    }

}
