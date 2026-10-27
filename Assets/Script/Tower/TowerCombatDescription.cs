using UnityEngine;

/// <summary>새 공격의 전투 설정을 능력치 창에서도 같은 데이터로 설명한다.</summary>
public static class TowerCombatDescription
{
    public static string Describe(TowerAttackProfile attack)
    {
        if (attack == null) return string.Empty;
        switch (attack.mode)
        {
            case TowerAttackMode.Laser:
                return $"지속 레이저 {attack.targets}줄 · 대상 고정\n단계별 피해 상승 · 최대 {attack.laserDamageCap} / 회\n보스 피해 {attack.bossDamage * 100:0}%";
            case TowerAttackMode.ChargedLaser:
                return "충전 후 에너지포 발사 · 충전 유지" + (attack.splashRadius > 0
                    ? $"\n주변 피해 {attack.splashDamage * 100:0}% · 범위 {attack.splashRadius:0.#}" : "");
            case TowerAttackMode.Sniper:
                if (attack.fragments > 0)
                    return $"명중 후 파편 {attack.fragments}개 · {attack.fragmentAngle:0}°\n파편당 피해 {attack.fragmentDamage * 100:0}% · 중복 명중 가능";
                return "직선 관통 3명 · 피해 100% / 70% / 40%" +
                    (attack.extraHitDamage > 0 ? $"\n대상별 추가 폭발 {attack.extraHitDamage * 100:0}%" : "") +
                    (attack.burnDamage > 0 ? $"\n폭발 피해의 {attack.burnDamage * 100:0}% 화상 × {attack.burnTicks}" : "");
            case TowerAttackMode.Shock:
                return $"감전 정지 {attack.stunDuration:0.#}초 · {attack.targets}대상" +
                    (attack.burnDamage > 0 ? $"\n피해의 {attack.burnDamage * 100:0}% 화상 × {attack.burnTicks}" : "") +
                    (attack.slowReduction > 0 ? $"\n이동속도 {attack.slowReduction * 100:0}% 감소" : "");
            default: return string.Empty;
        }
    }
}
