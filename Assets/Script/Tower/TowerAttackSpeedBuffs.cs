using System;
using System.Collections.Generic;

/// <summary>Independent buff timers; strongest source in each group, additive bonuses across groups.</summary>
public sealed class TowerAttackSpeedBuffs
{
    private sealed class Buff
    {
        public string Group;
        public object Source;
        public float Bonus;
        public float EndTime;
    }

    private readonly List<Buff> buffs = new List<Buff>();
    private readonly Dictionary<string, float> groupBonuses =
        new Dictionary<string, float>(StringComparer.Ordinal);
    private float multiplier = 1f;
    private float nextExpiry = float.PositiveInfinity;

    public void Set(string group, object source, float bonus, float endTime, float now)
    {
        if (string.IsNullOrWhiteSpace(group) || source == null || bonus <= 0f ||
            float.IsNaN(bonus) || float.IsInfinity(bonus) || endTime <= now)
            return;

        Tick(now);
        foreach (Buff buff in buffs)
        {
            if (buff.Group != group || !ReferenceEquals(buff.Source, source)) continue;
            buff.Bonus = bonus;
            buff.EndTime = Math.Max(buff.EndTime, endTime);
            Recalculate();
            return;
        }
        buffs.Add(new Buff { Group = group, Source = source, Bonus = bonus, EndTime = endTime });
        Recalculate();
    }

    public void Remove(object source)
    {
        bool changed = false;
        for (int index = buffs.Count - 1; index >= 0; index--)
        {
            if (!ReferenceEquals(buffs[index].Source, source)) continue;
            buffs.RemoveAt(index);
            changed = true;
        }
        if (changed) Recalculate();
    }

    public float Tick(float now)
    {
        if (now < nextExpiry) return multiplier;
        for (int index = buffs.Count - 1; index >= 0; index--)
            if (buffs[index].EndTime <= now) buffs.RemoveAt(index);
        Recalculate();
        return multiplier;
    }

    public bool ContainsGroup(string group) => groupBonuses.ContainsKey(group);

    public void Clear()
    {
        buffs.Clear();
        Recalculate();
    }

    private void Recalculate()
    {
        groupBonuses.Clear();
        nextExpiry = float.PositiveInfinity;
        foreach (Buff buff in buffs)
        {
            nextExpiry = Math.Min(nextExpiry, buff.EndTime);
            if (!groupBonuses.TryGetValue(buff.Group, out float bonus) || buff.Bonus > bonus)
                groupBonuses[buff.Group] = buff.Bonus;
        }
        multiplier = 1f;
        foreach (float bonus in groupBonuses.Values) multiplier += bonus;
    }
}
