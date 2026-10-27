using System;
using UnityEngine;

public enum ResearchTargetType
{
    Tower,
    Skill
}

public enum ResearchModifierType
{
    DamageFlat,
    DamagePercent,
    AttackCooldownPercent,
    AttackRangePercent,
    PeriodicDamagePercent,
    DurationFlat,
    AttackSpeedBonusFlat
}

[Serializable]
public struct ResearchRank
{
    [Min(1)] public int skillPointCost;
    [Tooltip("DamageFlat은 고정 피해량, DurationFlat은 초, AttackSpeedBonusFlat은 공속 증가율의 %p, 나머지는 % 단위입니다.")]
    public float modifierValue;
}

/// <summary>
/// 전술 연구소에서 구매하는 영구 성장 한 항목입니다.
/// 원본 TowerData/SkillData는 수정하지 않고, 구매 단계만 저장해 전투 수치에 계산합니다.
/// </summary>
[CreateAssetMenu(fileName = "ResearchData", menuName = "Last of the Tower/Research Data")]
public sealed class ResearchData : ScriptableObject
{
    [Header("식별")]
    [SerializeField] private string researchId;
    [SerializeField] private ResearchTargetType targetType;
    [Tooltip("TowerData.id 또는 SkillData.SkillId")]
    [SerializeField] private string targetId;
    [SerializeField] private ResearchModifierType modifierType;
    [Tooltip("연구소 화면에서 이름·아이콘·기본 수치를 표시하기 위한 참조입니다.")]
    [SerializeField] private TowerData towerTarget;
    [Tooltip("연구소 화면에서 이름·아이콘·기본 수치를 표시하기 위한 참조입니다.")]
    [SerializeField] private SkillData skillTarget;

    [Header("표시")]
    [SerializeField] private string displayName;
    [TextArea(2, 4)] [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("단계")]
    [SerializeField] private ResearchRank[] ranks = Array.Empty<ResearchRank>();

    public string ResearchId => researchId;
    public ResearchTargetType TargetType => targetType;
    public string TargetId => targetId;
    public ResearchModifierType ModifierType => modifierType;
    public TowerData TowerTarget => towerTarget;
    public SkillData SkillTarget => skillTarget;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxLevel => ranks?.Length ?? 0;

    public ResearchRank GetRank(int level)
    {
        if (ranks == null || level < 0 || level >= ranks.Length)
            return default;

        return ranks[level];
    }

    public float GetTotalModifierValue(int purchasedLevel)
    {
        float total = 0f;
        int validLevel = Mathf.Clamp(purchasedLevel, 0, MaxLevel);
        for (int index = 0; index < validLevel; index++)
            total += ranks[index].modifierValue;

        return total;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        string configuredResearchId,
        ResearchTargetType configuredTargetType,
        string configuredTargetId,
        ResearchModifierType configuredModifierType,
        string configuredDisplayName,
        string configuredDescription,
        Sprite configuredIcon,
        ResearchRank[] configuredRanks,
        TowerData configuredTowerTarget,
        SkillData configuredSkillTarget)
    {
        researchId = configuredResearchId;
        targetType = configuredTargetType;
        targetId = configuredTargetId;
        modifierType = configuredModifierType;
        displayName = configuredDisplayName;
        description = configuredDescription;
        icon = configuredIcon;
        ranks = configuredRanks ?? Array.Empty<ResearchRank>();
        towerTarget = configuredTowerTarget;
        skillTarget = configuredSkillTarget;
        OnValidate();
    }
#endif

    private void OnValidate()
    {
        researchId = NormalizeId(researchId);
        targetId = NormalizeId(targetId);
        if (towerTarget != null && targetType == ResearchTargetType.Tower)
            targetId = NormalizeId(towerTarget.id);
        if (skillTarget != null && targetType == ResearchTargetType.Skill)
            targetId = NormalizeId(skillTarget.SkillId);
        displayName = displayName?.Trim();

        if (ranks == null)
            ranks = Array.Empty<ResearchRank>();

        for (int index = 0; index < ranks.Length; index++)
            ranks[index].skillPointCost = Mathf.Max(1, ranks[index].skillPointCost);
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant().Replace(' ', '_');
    }
}
