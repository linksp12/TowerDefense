using System.Collections.Generic;
using UnityEngine;

/// <summary>조준 중 유효 대상에만 표시한다. 실제 효과를 적용하지 않는다.</summary>
internal sealed class SkillTargetPreview
{
    private sealed class Entry
    {
        public SpriteRenderer Visual, Source, Silhouette, HeadMarker;
    }
    private readonly Dictionary<Component, Entry> entries = new Dictionary<Component, Entry>();
    private readonly HashSet<Component> current = new HashSet<Component>();
    private readonly List<Component> removed = new List<Component>();
    private readonly List<MonsterHealth> monsters = new List<MonsterHealth>();
    private SkillData selected;

    public bool Refresh(SkillData skill, Vector3 center, float radius)
    {
        if (skill != selected) { Clear(); selected = skill; }
        if (skill == null || skill.VisualProfile == null) return false;
        current.Clear();
        if (skill.EffectType == SkillEffectType.TowerHaste)
        {
            foreach (TowerAttack tower in TowerAttack.ActiveTowers)
                if (tower != null && tower.isActiveAndEnabled && tower.towerData != null &&
                    ((Vector2)(tower.transform.position - center)).sqrMagnitude <= radius * radius) current.Add(tower);
        }
        else
        {
            SkillTargetQuery.Collect(skill, center, radius, monsters);
            foreach (MonsterHealth monster in monsters) current.Add(monster);
        }
        removed.Clear();
        foreach (var pair in entries) if (!current.Contains(pair.Key)) removed.Add(pair.Key);
        foreach (Component key in removed) { Remove(entries[key]); entries.Remove(key); }
        foreach (Component target in current)
        {
            if (!entries.TryGetValue(target, out Entry entry))
            {
                entry = new Entry { Source = SkillVisuals.MainRenderer(target),
                    Visual = SkillVisuals.Create(skill.SkillId + " Target", target.transform) };
                if (skill.EffectType == SkillEffectType.Fire)
                    entry.Silhouette = SkillVisuals.CreateSilhouette(entry.Source, skill.VisualProfile, entry.Visual.transform);
                if (skill.VisualProfile.targetHeadMarker != null)
                {
                    entry.HeadMarker = SkillVisuals.Create(skill.SkillId + " Head Lock", entry.Visual.transform);
                    entry.HeadMarker.sprite = skill.VisualProfile.targetHeadMarker;
                }
                entries.Add(target, entry);
            }
            Update(entry, skill);
        }
        return current.Count > 0;
    }

    private static void Update(Entry entry, SkillData skill)
    {
        if (entry.Source == null) return;
        SkillVisualProfile profile = skill.VisualProfile;
        if (profile.targetFrames == null || profile.targetFrames.Length == 0) return;
        entry.Visual.sprite = profile.targetFrames[(int)(Time.unscaledTime * profile.targetFps) % profile.targetFrames.Length];
        Bounds bounds = entry.Source.bounds;
        bool tower = skill.EffectType == SkillEffectType.TowerHaste;
        SkillVisuals.FitWidth(entry.Visual, bounds.size.x * profile.targetWidthMultiplier);
        float height = bounds.min.y + (tower ? 0.05f : 0);
        entry.Visual.transform.position = new Vector3(bounds.center.x, height, bounds.center.z);
        if (entry.HeadMarker != null)
        {
            SkillVisuals.FitWidth(entry.HeadMarker, bounds.size.x * profile.targetHeadWidthMultiplier);
            entry.HeadMarker.transform.position = new Vector3(bounds.center.x,
                bounds.max.y + profile.targetHeadGap, bounds.center.z);
        }
        Color tint = profile.accent; tint.a = 0.3f;
        SkillVisuals.FollowSilhouette(entry.Silhouette, entry.Source, tint);
    }

    private static void Remove(Entry entry)
    {
        if (entry.Visual == null) return;
        entry.Visual.gameObject.SetActive(false);
        Object.Destroy(entry.Visual.gameObject);
    }

    public void Clear()
    {
        foreach (Entry entry in entries.Values) Remove(entry);
        entries.Clear(); current.Clear(); selected = null;
    }
}
