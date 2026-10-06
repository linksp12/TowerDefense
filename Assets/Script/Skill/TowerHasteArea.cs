using System.Collections.Generic;
using UnityEngine;

/// <summary>A timed haste field. Checks registered towers without repeated scene searches.</summary>
public sealed class TowerHasteArea : MonoBehaviour
{
    private const float CheckInterval = 0.1f;
    private readonly HashSet<TowerAttack> affected = new HashSet<TowerAttack>();
    private readonly List<TowerAttack> removed = new List<TowerAttack>();
    private SkillData skill;
    private float bonus;
    private float nextCheck;
    private SpriteRenderer visual;
    private Color visualColor;
    private bool initialized;

    public float Radius { get; private set; }
    public float EndTime { get; private set; }

    public void Initialize(SkillData data, float radius, float speedBonus, float duration)
    {
        skill = data;
        Radius = radius;
        bonus = speedBonus;
        EndTime = Time.time + duration;
        visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null)
        {
            visual.sortingLayerID = SortingLayer.NameToID("Effects");
            visual.sortingOrder = 4;
            visualColor = visual.color;
            visualColor.a = 0.45f;
            visual.color = visualColor;
            SkillRangeVisual.Fit(visual, radius, data.EffectRadiusFraction);
        }
        initialized = true;
        RefreshTargets();
    }

    private void Update()
    {
        if (!initialized) return;
        if (Time.time >= EndTime)
        {
            ReleaseTargets();
            Destroy(gameObject);
            return;
        }
        if (Time.time >= nextCheck) RefreshTargets();
        if (visual != null)
        {
            Color color = visualColor;
            color.a *= Mathf.Clamp01((EndTime - Time.time) / 0.35f);
            visual.color = color;
        }
    }

    private bool Contains(TowerAttack tower)
    {
        return tower != null && tower.isActiveAndEnabled && tower.towerData != null &&
            ((Vector2)(tower.transform.position - transform.position)).sqrMagnitude <= Radius * Radius;
    }

    private void RefreshTargets()
    {
        nextCheck = Time.time + CheckInterval;
        foreach (TowerAttack tower in TowerAttack.ActiveTowers)
        {
            if (!Contains(tower)) continue;
            tower.ApplyAttackSpeedBuff(skill.SkillId, this, bonus, EndTime - Time.time,
                skill.BuffMarkerPrefab, skill.BuffMarkerHeight);
            affected.Add(tower);
        }
        removed.Clear();
        foreach (TowerAttack tower in affected)
            if (!Contains(tower)) removed.Add(tower);
        foreach (TowerAttack tower in removed)
        {
            if (tower != null) tower.RemoveAttackSpeedBuff(this);
            affected.Remove(tower);
        }
    }

    private void ReleaseTargets()
    {
        foreach (TowerAttack tower in affected)
            if (tower != null) tower.RemoveAttackSpeedBuff(this);
        affected.Clear();
    }

    private void OnDisable() => ReleaseTargets();
}
