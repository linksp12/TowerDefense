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
        public LineRenderer Line;
    }

    private readonly TowerAttack tower;
    private readonly SpriteRenderer towerRenderer;
    private readonly List<Beam> beams = new List<Beam>();
    private readonly List<MonsterHealth> selected = new List<MonsterHealth>();
    private TowerAttackProfile profile;
    private float timer;
    private SpriteRenderer charge;
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
            if (beam.Line != null) { beam.Line.enabled = false; Object.Destroy(beam.Line.gameObject); }
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
        var target = TowerTargeting.Nearest(tower);
        if (target == null) return; // 완충 상태는 대상이 없어도 유지한다.
        timer = 0;
        if (profile.mode == TowerAttackMode.Sniper) FireSniper(target);
        else if (profile.mode == TowerAttackMode.Shock) FireShock();
        else if (profile.mode == TowerAttackMode.ChargedLaser)
        {
            TowerEnergyShot.Fire(tower.ShotOrigin, target, EnvDamage(tower.damage), profile, tower.canDetectStealth);
            if (TowerVfx.Settings != null) TowerVfx.Play(TowerVfx.Settings.laserShot, tower.ShotOrigin);
        }
    }

    private void FireSniper(MonsterHealth target)
    {
        int damage = EnvDamage(tower.damage);
        if (Random.value < tower.criticalChance) damage = Mathf.RoundToInt(damage * tower.criticalDamageMultiplier);
        Vector2 direction = target.transform.position - tower.ShotOrigin;
        TowerPiercingShot.Fire(tower.ShotOrigin, direction, damage, profile, tower.attackRange + 2,
            tower.canDetectStealth);
        if (TowerVfx.Settings != null)
        {
            TowerVfx.Play(TowerVfx.Settings.sniperShot, tower.ShotOrigin);
            TowerVfx.Burst(TowerVfx.Settings.energyImpact, tower.ShotOrigin, 0.2f, 0.07f);
        }
    }

    private void FireShock()
    {
        selected.Clear();
        for (int i = 0; i < Mathf.Max(1, profile.targets); i++)
        {
            var target = TowerTargeting.Nearest(tower, selected);
            if (target == null) break;
            selected.Add(target);
            Vector3 origin = EmitterPosition(i, profile.targets);
            TowerVfx.Flash(origin, target.transform.position, TowerVfx.ElectricColor, true);
            int damage = EnvDamage(tower.damage);
            target.TakeDamage(damage, false, tower.criticalChance, tower.criticalDamageMultiplier);
            if (target.IsDead) continue;
            target.GetComponent<MonsterMove>()?.ApplyShock(profile.stunDuration,
                profile.slowReduction, profile.slowDuration);
            TowerBurnStack.Apply(target, Mathf.RoundToInt(damage * profile.burnDamage),
                profile.burnTicks, profile.burnInterval);
            if (TowerVfx.Settings != null)
                TowerVfx.Burst(TowerVfx.Settings.shockImpact, target.transform.position, 0.4f, 0.12f);
        }
        if (TowerVfx.Settings != null) TowerVfx.Play(TowerVfx.Settings.electricShot, tower.ShotOrigin);
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
                beam.Target = TowerTargeting.Nearest(tower, selected);
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
        beam.Target.TakeDamage(EnvDamage(damage), false);
        if (TowerVfx.Settings != null)
            TowerVfx.Burst(TowerVfx.Settings.energyImpact, beam.Target.transform.position, 0.2f, 0.08f);
    }

    private void UpdateBeamVisual(Beam beam, int index, int count)
    {
        if (beam.Target == null || beam.Target.IsDead)
        {
            if (beam.Line != null) beam.Line.enabled = false;
            return;
        }
        if (beam.Line == null)
        {
            var go = new GameObject("Sustained Laser");
            go.transform.SetParent(tower.transform, false);
            beam.Line = TowerVfx.Line(go, TowerVfx.LaserColor, 0.025f);
            beam.Line.positionCount = 2;
        }
        beam.Line.enabled = true;
        int steps = Mathf.Max(1, profile.laserRamp != null ? profile.laserRamp.Length - 1 : 1);
        float strength = Mathf.Clamp01(beam.Step / (float)steps);
        beam.Line.startWidth = beam.Line.endWidth = Mathf.Lerp(0.025f, 0.075f, strength);
        beam.Line.SetPosition(0, EmitterPosition(index, count));
        beam.Line.SetPosition(1, beam.Target.transform.position);
    }

    private void UpdateCharge()
    {
        if (charge == null && TowerVfx.Settings != null && TowerVfx.Settings.energyBolt != null)
        {
            var go = new GameObject("Stored Laser Charge");
            go.transform.SetParent(tower.transform, false);
            charge = TowerVfx.Renderer(go, TowerVfx.Settings.energyBolt, 0.3f);
        }
        if (charge == null) return;
        charge.transform.position = tower.ShotOrigin;
        charge.color = new Color(1, 0.55f, 0.45f, ChargeNormalized * 0.85f);
    }
}
