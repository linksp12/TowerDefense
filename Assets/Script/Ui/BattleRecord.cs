using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 전투의 플레이 기록: 처치 수, 타워 종류별 설치 수·준 피해, 전투 시간.
/// GameManager가 전투마다 하나 만들고, 결과창(타워별 피해량)과 누적 플레이 기록(PlayRecord)이 읽는다.
/// </summary>
public sealed class BattleRecord
{
    public sealed class TowerDamage
    {
        public string Id;
        public string Name;
        public Sprite Icon;
        public long Damage;
        public readonly HashSet<TowerAttack> Towers = new HashSet<TowerAttack>();
    }

    private readonly Dictionary<string, TowerDamage> damage = new Dictionary<string, TowerDamage>(System.StringComparer.Ordinal);
    private readonly Dictionary<string, int> builds = new Dictionary<string, int>(System.StringComparer.Ordinal);
    private readonly Dictionary<string, string> towerNames = new Dictionary<string, string>(System.StringComparer.Ordinal);
    private readonly float startedAt = Time.unscaledTime;
    private float endedAt = -1f;

    public int Kills { get; private set; }
    public long TotalDamage { get; private set; }
    public IReadOnlyDictionary<string, int> Builds => builds;
    public IReadOnlyDictionary<string, string> TowerNames => towerNames;

    /// <summary>전투 시작부터 결과까지 실제 흐른 시간(초). 결과 전에는 지금까지의 시간.</summary>
    public float Seconds => (endedAt >= 0f ? endedAt : Time.unscaledTime) - startedAt;

    public void Finish()
    {
        if (endedAt < 0f)
            endedAt = Time.unscaledTime;
    }

    public void RecordKill()
    {
        if (endedAt < 0f)
            Kills++;
    }

    public void RecordBuild(TowerData data)
    {
        if (endedAt >= 0f || data == null || string.IsNullOrEmpty(data.id))
            return;
        builds.TryGetValue(data.id, out int count);
        builds[data.id] = count + 1;
        towerNames[data.id] = data.towerName;
    }

    // 판매·파괴된 타워도 TowerData 참조는 남아 있어 종류로 묶을 수 있다.
    public void RecordDamage(TowerAttack tower, int amount)
    {
        if (endedAt >= 0f || amount <= 0 || ReferenceEquals(tower, null) || tower.towerData == null)
            return;
        TowerData data = tower.towerData;
        if (!damage.TryGetValue(data.id, out TowerDamage entry))
        {
            entry = new TowerDamage { Id = data.id, Name = data.towerName, Icon = data.icon };
            damage[data.id] = entry;
        }
        if (entry.Icon == null && tower != null && tower.TryGetComponent(out SpriteRenderer renderer))
            entry.Icon = renderer.sprite;
        entry.Damage += amount;
        entry.Towers.Add(tower);
        TotalDamage += amount;
    }

    /// <summary>타워 종류별 피해량, 많은 순.</summary>
    public List<TowerDamage> DamageByTower()
    {
        var list = new List<TowerDamage>(damage.Values);
        list.Sort((a, b) => b.Damage.CompareTo(a.Damage));
        return list;
    }
}
