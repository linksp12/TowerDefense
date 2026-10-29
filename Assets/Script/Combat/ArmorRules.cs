using UnityEngine;

/// <summary>타워 피해 종류. 화살·캐논·저격은 물리, 마법·레이저·감전은 마법.</summary>
public enum DamageKind
{
    Physical,
    Magic
}

/// <summary>몬스터 방어 특성. 맞는 종류의 피해를 일정 비율 덜 받는다.</summary>
public enum ArmorTrait
{
    None,
    Armored,    // 물리 방어
    Warded      // 마법 저항
}

/// <summary>방어 특성 규칙과 표시 문구를 한곳에서 정한다(전투·도감·UI가 같은 값을 쓴다).</summary>
public static class ArmorRules
{
    public const float Reduction = 0.3f;

    public static bool Resists(ArmorTrait trait, DamageKind kind) =>
        (trait == ArmorTrait.Armored && kind == DamageKind.Physical) ||
        (trait == ArmorTrait.Warded && kind == DamageKind.Magic);

    public static int Apply(int damage, ArmorTrait trait, DamageKind kind) =>
        Resists(trait, kind) ? Mathf.Max(1, Mathf.RoundToInt(damage * (1f - Reduction))) : damage;

    public static string TraitName(ArmorTrait trait) => trait switch
    {
        ArmorTrait.Armored => "물리 방어",
        ArmorTrait.Warded => "마법 저항",
        _ => ""
    };

    public static string TraitDescription(ArmorTrait trait) => trait switch
    {
        ArmorTrait.Armored => $"화살·캐논·저격 피해 {Mathf.RoundToInt(Reduction * 100)}% 감소",
        ArmorTrait.Warded => $"마법·레이저·감전 피해 {Mathf.RoundToInt(Reduction * 100)}% 감소",
        _ => ""
    };

    // 도감 칸처럼 좁은 곳에 쓰는 짧은 설명
    public static string ShortDescription(ArmorTrait trait) => trait switch
    {
        ArmorTrait.Armored => $"물리 피해 -{Mathf.RoundToInt(Reduction * 100)}%",
        ArmorTrait.Warded => $"마법 피해 -{Mathf.RoundToInt(Reduction * 100)}%",
        _ => ""
    };

    public static string KindName(DamageKind kind) => kind == DamageKind.Magic ? "마법 피해" : "물리 피해";
}
