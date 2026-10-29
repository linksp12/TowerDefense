using System.Collections.Generic;
using UnityEngine;

/// <summary>특수 공격의 전투 상태. 공통 타워의 강화·가속·비활성화 생명주기를 그대로 사용한다.</summary>
internal sealed class TowerSpecialAttack
{
    private sealed class Beam
    {
        public MonsterHealth Target;
        public int Step;
        public float Timer;
        public TowerLaserBeam Visual;
    }

    private readonly TowerAttack tower;
    private readonly SpriteRenderer towerRenderer;
    private readonly List<Beam> beams = new List<Beam>();
    private readonly List<MonsterHealth> selected = new List<MonsterHealth>();
    private TowerAttackProfile profile;
    private float timer;
    private PixelLoop charge;
    public float ChargeNormalized => profile == null ? 0 : Mathf.Clamp01(timer / Mathf.Max(0.05f, tower.attackCooldown));

    public TowerSpecialAttack(TowerAttack tower)
    {
        this.tower = tower;
        towerRenderer = tower.GetComponent<SpriteRenderer>();
    }

    private Vector3 EmitterPosition(int index, int count)
    {
        if (count <= 1 || towerRenderer == null) return tower.ShotOrigin;
        Bounds bounds = towerRenderer.bounds;
        float x = bounds.center.x + (index - (count - 1) / 2f) * bounds.size.x * 0.27f;
        float inset = count == 3 && index != 1 ? 0.18f : 0.08f;
        return new Vector3(x, bounds.max.y - bounds.size.y * inset, tower.transform.position.z);
    }

    // 특수 공격 피해에도 스테이지 환경의 속성 보정을 적용한다(일반 투사체와 같은 규칙).
    private int EnvDamage(int damage) => EnvironmentManager.ApplyToDamage(damage, tower.Element);

    public void Reset()
    {
        foreach (var beam in beams)
            beam.Visual?.Destroy();
        beams.Clear();
        if (charge != null) Object.Destroy(charge.gameObject);
        charge = null;
        timer = 0;
        profile = null;
    }

    public void Tick(float scaledDelta)
    {
        if (profile != tower.CurrentAttackProfile)
        {
            Reset();
            profile = tower.CurrentAttackProfile;
        }
        if (profile == null) return;
        if (profile.mode == TowerAttackMode.Laser) { TickBeams(scaledDelta); return; }
        timer = Mathf.Min(timer + scaledDelta, Mathf.Max(0.05f, tower.attackCooldown));
        if (profile.mode == TowerAttackMode.ChargedLaser) UpdateCharge();
        if (timer + 0.00001f < tower.attackCooldown) return;
        var target = TowerTargeting.Select(tower);
        if (target == null) return; // 완충 상태는 대상이 없어도 유지한다.
        timer = 0;
        if (profile.mode == TowerAttackMode.Sniper) FireSniper(target);
        else if (profile.mode == TowerAttackMode.Shock) FireShock();
        else if (profile.mode == TowerAttackMode.ChargedLaser)
        {
            TowerEnergyShot.Fire(tower.ShotOrigin, target, EnvDamage(tower.damage), profile, tower.canDetectStealth);
            CombatFeedback.LaserChargeFired(tower.ShotOrigin);
        }
    }

    private void FireSniper(MonsterHealth target)
    {
        int damage = EnvDamage(tower.damage);
        bool critical = Random.value < tower.criticalChance;
        if (critical) damage = Mathf.RoundToInt(damage * tower.criticalDamageMultiplier);
        Vector2 direction = target.transform.position - tower.ShotOrigin;
        TowerPiercingShot.Fire(tower.ShotOrigin, direction, damage, profile, tower.EffectiveRange + 2,
            tower.canDetectStealth, critical: critical);
        CombatFeedback.SniperFired(tower.ShotOrigin, critical);
    }

    private void FireShock()
    {
        selected.Clear();
        for (int i = 0; i < Mathf.Max(1, profile.targets); i++)
        {
            var target = TowerTargeting.Select(tower, selected);
            if (target == null) break;
            selected.Add(target);
            Vector3 hit = TowerBodyBounds.Of(target.gameObject).center;
            TowerElectricArc.Spawn(EmitterPosition(i, profile.targets), hit);
            CombatFeedback.ShockHit(hit);
            int damage = EnvDamage(tower.damage);
            target.TakeDamage(target.Resist(damage, DamageKind.Magic), false, tower.criticalChance, tower.criticalDamageMultiplier);
            if (target.IsDead) continue;
            target.GetComponent<MonsterMove>()?.ApplyShock(profile.stunDuration,
                profile.slowReduction, profile.slowDuration);
            if (profile.stunDuration > 0) MonsterStatusMarkers.Attach(target.gameObject);
            TowerBurnStack.Apply(target, Mathf.RoundToInt(damage * profile.burnDamage),
                profile.burnTicks, profile.burnInterval);
        }
        if (selected.Count > 0) CombatFeedback.ShockFired();
    }

    private void TickBeams(float delta)
    {
        int count = Mathf.Clamp(profile.targets, 1, 8);
        while (beams.Count < count) beams.Add(new Beam());
        selected.Clear();
        foreach (var beam in beams)
        {
            if (!TowerTargeting.InRange(beam.Target, tower))
            {
                beam.Target = null;
                beam.Step = 0;
                beam.Timer = 0;
            }
            if (beam.Target != null) selected.Add(beam.Target);
        }
        for (int i = 0; i < count; i++)
        {
            Beam beam = beams[i];
            if (beam.Target == null)
            {
                beam.Target = TowerTargeting.Select(tower, selected);
                if (beam.Target != null) { selected.Add(beam.Target); DamageBeam(beam); }
            }
            else
            {
                beam.Timer += delta;
                float interval = Mathf.Max(0.05f, tower.attackCooldown);
                while (beam.Timer >= interval && TowerTargeting.InRange(beam.Target, tower))
                {
                    beam.Timer -= interval;
                    beam.Step++;
                    DamageBeam(beam);
                }
            }
            UpdateBeamVisual(beam, i, count);
        }
    }

    private void DamageBeam(Beam beam)
    {
        float[] ramp = profile.laserRamp;
        float multiplier = ramp != null && ramp.Length > 0 ? ramp[Mathf.Min(beam.Step, ramp.Length - 1)] : 1;
        int cap = ResearchStatResolver.GetTowerDamage(tower.towerData.id, profile.laserDamageCap);
        int damage = Mathf.Min(cap, Mathf.RoundToInt(tower.damage * multiplier));
        if (beam.Target.isBoss) damage = Mathf.RoundToInt(damage * profile.bossDamage);
        Vector3 hit = TowerBodyBounds.Of(beam.Target.gameObject).center;
        beam.Target.TakeDamage(beam.Target.Resist(EnvDamage(damage), DamageKind.Magic), false);
        CombatFeedback.LaserTick(hit, BeamStrength(beam));
    }

    // 레이저 단계(0~1). 오래 맞힐수록 빔이 굵어지고 달아오른다.
    private float BeamStrength(Beam beam)
    {
        int steps = Mathf.Max(1, profile.laserRamp != null ? profile.laserRamp.Length - 1 : 1);
        return Mathf.Clamp01(beam.Step / (float)steps);
    }

    private void UpdateBeamVisual(Beam beam, int index, int count)
    {
        if (beam.Target == null || beam.Target.IsDead)
        {
            beam.Visual?.Hide();
            return;
        }
        if (beam.Visual == null) beam.Visual = new TowerLaserBeam(tower.transform);
        beam.Visual.Show(EmitterPosition(index, count), TowerBodyBounds.Of(beam.Target.gameObject).center,
            BeamStrength(beam));
    }

    // 충전 레이저: 탑 위에 에너지 구슬이 점점 커지고, 다 차면 크게 맥동한다.
    private void UpdateCharge()
    {
        if (charge == null)
        {
            CombatFeelSettings fx = CombatFeedback.Settings;
            charge = fx != null ? PixelLoop.Create("Stored Laser Charge", tower.transform, fx.energyChargeFx, 105) : null;
            if (charge == null) return;
        }
        float fill = ChargeNormalized;
        float pulse = fill >= 1f ? 1f + 0.12f * Mathf.Sin(Time.time * 14f) : 1f;
        charge.transform.position = tower.ShotOrigin;
        charge.SetWorldScale(Mathf.Lerp(0.3f, 0.9f, fill) * pulse);
        charge.Renderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 1f, fill));
    }
}
