using System;
using UnityEngine;

public enum TowerAttackMode { Projectile, Sniper, Shock, Laser, ChargedLaser }

/// <summary>공격 방식별 설정. 진행 상태와 타이머는 전투 객체가 소유한다.</summary>
[Serializable]
public sealed class TowerAttackProfile
{
    public TowerAttackMode mode;
    [Min(1)] public int targets = 1;
    [Min(1)] public int pierceCount = 3;
    public float[] pierceDamage = { 1f, 0.7f, 0.4f };
    [Min(0)] public int fragments;
    [Range(0, 180)] public float fragmentAngle = 90;
    [Range(0, 1)] public float fragmentDamage = 0.5f;
    [Range(0, 1)] public float extraHitDamage;
    [Range(0, 1)] public float burnDamage;
    [Min(1)] public int burnTicks = 5;
    [Min(0.05f)] public float burnInterval = 0.4f;
    [Min(0)] public float stunDuration;
    [Range(0, 1)] public float slowReduction;
    [Min(0)] public float slowDuration;
    [Min(1)] public float projectileSpeed = 22f;
    [Min(0)] public float splashRadius;
    [Range(0, 1)] public float splashDamage = 0.8f;
    [Range(0, 1)] public float bossDamage = 0.5f;
    [Min(1)] public int laserDamageCap = 2000;
    public float[] laserRamp = { 1, 1.5f, 3, 5, 8.5f, 14, 23, 38,
        62.5f, 103, 170, 280.5f, 462.5f, 500 };

    public float PierceMultiplier(int index)
    {
        return pierceDamage != null && index < pierceDamage.Length
            ? Mathf.Max(0, pierceDamage[index]) : 1f;
    }
}
