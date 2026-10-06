using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 저장된 성장 단계를 원본 전투 수치에 한 번만 반영합니다.
/// 이 클래스는 원본 ScriptableObject 값을 절대 변경하지 않습니다.
/// </summary>
public static class ResearchStatResolver
{
    private static readonly Dictionary<string, ResearchData> ResearchById =
        new Dictionary<string, ResearchData>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<ResearchData>> ResearchByStat =
        new Dictionary<string, List<ResearchData>>(StringComparer.OrdinalIgnoreCase);
    private static bool isLoaded;

    public static int GetTowerDamage(string towerId, int baseDamage)
    {
        return ApplyDamage(baseDamage, ResearchTargetType.Tower, towerId);
    }

    public static float GetTowerAttackCooldown(string towerId, float baseCooldown)
    {
        float percentage = GetTotalPercent(
            ResearchTargetType.Tower, towerId, ResearchModifierType.AttackCooldownPercent);
        return Mathf.Max(0.05f, baseCooldown * (1f - percentage / 100f));
    }

    public static float GetTowerAttackRange(string towerId, float baseRange)
    {
        return ApplyPercent(baseRange, ResearchTargetType.Tower, towerId,
            ResearchModifierType.AttackRangePercent);
    }

    public static int GetSkillDamage(SkillData skill)
    {
        return skill == null ? 0 : ApplyDamage(skill.Damage, ResearchTargetType.Skill, skill.SkillId);
    }

    public static int GetSkillPeriodicDamage(SkillData skill)
    {
        if (skill == null)
            return 0;

        return Mathf.Max(0, Mathf.RoundToInt(ApplyPercent(skill.PeriodicDamage,
            ResearchTargetType.Skill, skill.SkillId, ResearchModifierType.PeriodicDamagePercent)));
    }

    public static float GetSkillDuration(SkillData skill)
    {
        return skill == null ? 0f : Mathf.Max(0f, skill.Duration + GetTotalValue(
            ResearchTargetType.Skill, skill.SkillId, ResearchModifierType.DurationFlat));
    }

    public static float GetSkillAttackSpeedBonus(SkillData skill)
    {
        return skill == null ? 0f : Mathf.Max(0f, skill.AttackSpeedBonus + GetTotalValue(
            ResearchTargetType.Skill, skill.SkillId, ResearchModifierType.AttackSpeedBonusFlat) / 100f);
    }

    public static float GetTotalPercent(
        ResearchTargetType targetType,
        string targetId,
        ResearchModifierType modifierType)
    {
        return GetTotalValue(targetType, targetId, modifierType);
    }

    public static float GetTotalValue(
        ResearchTargetType targetType,
        string targetId,
        ResearchModifierType modifierType)
    {
        return GetTotalValue(targetType, targetId, modifierType, null, 0);
    }

    private static float GetTotalValue(
        ResearchTargetType targetType, string targetId, ResearchModifierType modifierType,
        ResearchData previewResearch, int previewLevel)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return 0f;

        EnsureLoaded();
        float total = 0f;
        if (!ResearchByStat.TryGetValue(CreateStatKey(targetType, targetId, modifierType), out List<ResearchData> growths))
            return 0f;

        foreach (ResearchData growth in growths)
        {
            if (growth == null)
                continue;

            int level = previewResearch != null && growth.ResearchId == previewResearch.ResearchId
                ? previewLevel : ResearchProgressStore.GetLevel(growth.ResearchId);
            total += growth.GetTotalModifierValue(level);
        }

        return total;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static void ReloadForEditorAndTests()
    {
        isLoaded = false;
        ResearchById.Clear();
        ResearchByStat.Clear();
    }
#endif

    private static int ApplyDamage(int baseValue, ResearchTargetType targetType, string targetId)
    {
        return ApplyDamage(baseValue, targetType, targetId, null, 0);
    }

    private static int ApplyDamage(int baseValue, ResearchTargetType targetType, string targetId,
        ResearchData previewResearch, int previewLevel)
    {
        float flatBonus = GetTotalValue(targetType, targetId, ResearchModifierType.DamageFlat,
            previewResearch, previewLevel);
        float percent = GetTotalValue(targetType, targetId, ResearchModifierType.DamagePercent,
            previewResearch, previewLevel);
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * (1f + percent / 100f) + flatBonus));
    }

    /// <summary>선택한 연구의 단계만 가정해 계산합니다. 구매 기록이나 원본을 변경하지 않습니다.</summary>
    public static float GetResearchStatValue(ResearchData research, int level)
    {
        if (research == null)
            return 0f;

        float baseValue = GetResearchBaseValue(research);
        if (research.ModifierType == ResearchModifierType.DamageFlat ||
            research.ModifierType == ResearchModifierType.DamagePercent)
            return ApplyDamage(Mathf.RoundToInt(baseValue), research.TargetType, research.TargetId,
                research, level);

        float percent = GetTotalValue(research.TargetType, research.TargetId,
            research.ModifierType, research, level);
        if (research.ModifierType == ResearchModifierType.AttackCooldownPercent)
            return Mathf.Max(0.05f, baseValue * (1f - percent / 100f));
        if (research.ModifierType == ResearchModifierType.DurationFlat)
            return Mathf.Max(0f, baseValue + percent);
        if (research.ModifierType == ResearchModifierType.AttackSpeedBonusFlat)
            return Mathf.Max(0f, baseValue + percent);

        float value = baseValue * (1f + percent / 100f);
        return research.ModifierType == ResearchModifierType.PeriodicDamagePercent
            ? Mathf.Max(0, Mathf.RoundToInt(value)) : value;
    }

    public static float GetResearchBaseValue(ResearchData research)
    {
        if (research.TowerTarget != null)
        {
            TowerData.CombatStats stats = research.TowerTarget.baseStats;
            switch (research.ModifierType)
            {
                case ResearchModifierType.DamageFlat:
                case ResearchModifierType.DamagePercent: return stats.damage;
                case ResearchModifierType.AttackCooldownPercent: return stats.attackCooldown;
                case ResearchModifierType.AttackRangePercent: return stats.attackRange;
            }
        }

        if (research.SkillTarget != null)
        {
            switch (research.ModifierType)
            {
                case ResearchModifierType.PeriodicDamagePercent: return research.SkillTarget.PeriodicDamage;
                case ResearchModifierType.DurationFlat: return research.SkillTarget.Duration;
                case ResearchModifierType.AttackSpeedBonusFlat: return research.SkillTarget.AttackSpeedBonus * 100f;
                default: return research.SkillTarget.Damage;
            }
        }

        return 0f;
    }

    private static float ApplyPercent(
        float baseValue,
        ResearchTargetType targetType,
        string targetId,
        ResearchModifierType modifierType)
    {
        return baseValue * (1f + GetTotalPercent(targetType, targetId, modifierType) / 100f);
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        isLoaded = true;
        foreach (ResearchData growth in Resources.LoadAll<ResearchData>("Research"))
        {
            if (growth == null || string.IsNullOrWhiteSpace(growth.ResearchId))
                continue;

            if (ResearchById.ContainsKey(growth.ResearchId))
            {
                Debug.LogError($"중복된 연구 ID입니다: {growth.ResearchId}", growth);
                continue;
            }

            ResearchById.Add(growth.ResearchId, growth);
            string statKey = CreateStatKey(growth.TargetType, growth.TargetId, growth.ModifierType);
            if (!ResearchByStat.TryGetValue(statKey, out List<ResearchData> growths))
            {
                growths = new List<ResearchData>();
                ResearchByStat.Add(statKey, growths);
            }

            growths.Add(growth);
        }
    }

    private static string CreateStatKey(
        ResearchTargetType targetType,
        string targetId,
        ResearchModifierType modifierType)
    {
        return $"{targetType}|{targetId}|{modifierType}";
    }
}
