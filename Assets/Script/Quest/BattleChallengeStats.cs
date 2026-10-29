using System.Collections.Generic;

/// <summary>
/// 한 전투 동안 퀘스트·도전 과제 판정에 필요한 기록(설치한 타워 종류, 동시에 세운 최대 타워 수, 기지 피해 여부).
/// GameManager가 전투마다 하나 만들고, 결과 확정 때 QuestManager로 넘긴다.
/// </summary>
public sealed class BattleChallengeStats
{
    private readonly HashSet<string> builtTowerIds = new HashSet<string>(System.StringComparer.Ordinal);

    public IReadOnlyCollection<string> BuiltTowerIds => builtTowerIds;
    public bool HasBuilt(string towerId) => builtTowerIds.Contains(towerId);
    public int MaxTowers { get; private set; }
    public bool BaseDamaged { get; private set; }

    public void RecordBuild(string towerId, int towersStanding)
    {
        if (!string.IsNullOrEmpty(towerId))
            builtTowerIds.Add(towerId);
        if (towersStanding > MaxTowers)
            MaxTowers = towersStanding;
    }

    public void RecordBaseDamage() => BaseDamaged = true;
}
