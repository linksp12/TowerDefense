using System.Collections.Generic;
using UnityEngine;

/// <summary>스킬 설명창에 쓸 문구·능력치를 만듭니다. UI 생성과 전투 실행은 담당하지 않습니다.</summary>
internal static class SkillDescriptionFormatter
{
    /// <summary>속성 표식: (아이콘 이름, 글, 색)</summary>
    public static (string icon, string text, Color color) Kind(SkillData skill)
    {
        switch (skill.EffectType)
        {
            case SkillEffectType.Fire: return ("icon_flame", "화염 마법 · 광역", new Color32(255, 150, 90, 255));
            case SkillEffectType.Ice: return ("icon_snow", "빙결 마법 · 광역", new Color32(150, 215, 255, 255));
            case SkillEffectType.Lightning: return ("icon_bolt", "번개 마법 · 연쇄", new Color32(255, 226, 110, 255));
            default: return ("icon_fast", "지원 마법 · 모든 타워", new Color32(140, 232, 120, 255));
        }
    }

    /// <summary>
    /// 화면에 보일 이름. 스킬 데이터의 이름(Fireball 등)은 스테이지 버튼이 스킬을 찾는 열쇠이므로 바꾸지 않고,
    /// 영문이면 속성에 맞는 한글 이름으로 보여 준다.
    /// </summary>
    public static string Name(SkillData skill)
    {
        if (skill == null) return string.Empty;
        string name = skill.DisplayName ?? string.Empty;
        foreach (char c in name)
            if (c >= '가' && c <= '힣') return name;   // 이미 한글이면 그대로
        switch (skill.EffectType)
        {
            case SkillEffectType.Fire: return "화염구";
            case SkillEffectType.Ice: return "빙결 폭풍";
            case SkillEffectType.Lightning: return "연쇄 번개";
            case SkillEffectType.TowerHaste: return "전투 가속";
            default: return name;
        }
    }

    public static string Effect(SkillData skill) =>
        skill == null || string.IsNullOrWhiteSpace(skill.Description) ? "스킬 설명이 설정되지 않았습니다." : skill.Description;

    /// <summary>능력치 칸: (아이콘 이름, 이름, 값). 연구로 오른 최종 수치를 쓴다.</summary>
    public static List<(string icon, string label, string value)> Stats(SkillData skill)
    {
        var list = new List<(string, string, string)>();
        if (skill == null) return list;
        int damage = ResearchStatResolver.GetSkillDamage(skill);
        if (damage > 0) list.Add(("icon_sword", "피해량", damage.ToString()));
        int periodicDamage = ResearchStatResolver.GetSkillPeriodicDamage(skill);
        if (periodicDamage > 0)
            list.Add(("icon_flame", "지속 피해", $"{periodicDamage} / {skill.PeriodicInterval:0.##}초"));
        if (skill.EffectType == SkillEffectType.TowerHaste)
            list.Add(("icon_fast", "공격 속도", $"+{ResearchStatResolver.GetSkillAttackSpeedBonus(skill) * 100f:0.#}%"));
        if (skill.EffectType != SkillEffectType.TowerHaste)
            list.Add(("icon_target", "범위", skill.Range.ToString("0.##")));
        if (skill.MaxTargets > 0) list.Add(("icon_bolt", "최대 대상", $"{skill.MaxTargets}"));
        if (skill.Duration > 0f)
            list.Add(("icon_hourglass", "지속 시간", $"{ResearchStatResolver.GetSkillDuration(skill):0.##}초"));
        list.Add(("icon_restart", "재사용", $"{skill.Cooldown:0.##}초"));
        return list;
    }

    // 이전 글자형 설명(다른 곳에서 문자열이 필요할 때)
    public static string Format(SkillData skill)
    {
        if (skill == null) return string.Empty;
        var lines = new List<string> { $"<size=125%><b>{Name(skill)}</b></size>", "", "효과: " + Effect(skill) };
        foreach (var (_, label, value) in Stats(skill)) lines.Add($"{label}: {value}");
        return string.Join("\n", lines);
    }
}
