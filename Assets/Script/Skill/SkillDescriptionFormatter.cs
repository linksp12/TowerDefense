/// <summary>스킬의 설명 문구를 만듭니다. UI 생성과 전투 실행은 담당하지 않습니다.</summary>
internal static class SkillDescriptionFormatter
{
    public static string Format(SkillData skill)
    {
        if (skill == null) return string.Empty;

        string description = string.IsNullOrWhiteSpace(skill.Description)
            ? "스킬 설명이 설정되지 않았습니다." : skill.Description;
        string details = $"범위: {skill.Range:0.##}\n재사용 대기시간: {skill.Cooldown:0.##}초";
        int damage = ResearchStatResolver.GetSkillDamage(skill);
        if (damage > 0) details = $"피해량: {damage}\n" + details;
        if (skill.Duration > 0f)
            details += $"\n지속 시간: {ResearchStatResolver.GetSkillDuration(skill):0.##}초";
        if (skill.EffectType == SkillEffectType.TowerHaste)
            details += $"\n타워 공격 속도: +{ResearchStatResolver.GetSkillAttackSpeedBonus(skill) * 100f:0.#}%";
        if (skill.MaxTargets > 0) details += $"\n최대 대상 수: {skill.MaxTargets}";
        int periodicDamage = ResearchStatResolver.GetSkillPeriodicDamage(skill);
        if (periodicDamage > 0)
            details += $"\n지속 피해: {periodicDamage} / {skill.PeriodicInterval:0.##}초";

        return $"<size=28><b>{skill.DisplayName}</b></size>\n\n효과: {description}\n{details}";
    }
}
