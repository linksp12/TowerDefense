using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestData", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    public enum QuestKind
    {
        Board,            // 광장 게시판에서 수락하고, 달성 후 보상을 받는 퀘스트
        StageChallenge    // 스테이지마다 붙은 도전 과제(별). 수락 없이, 처음 달성하면 바로 보상
    }

    public string questId;
    public string questTitle;
    public string questDescription;
    public string targetStageId;

    [Tooltip("게시판 퀘스트 / 스테이지 도전 과제(별)")]
    public QuestKind kind = QuestKind.Board;

    [Tooltip("스테이지 도전 과제의 별 순서(1~3). 게시판 퀘스트는 0")]
    [Range(0, 3)] public int star;

    [Header("달성 조건 (모두 만족해야 함, 승리는 기본)")]
    [Tooltip("0이면 체력 조건 없음. 클리어 순간 남은 기지 체력이 이 값 이상이어야 달성")]
    [Min(0)] public int minRemainingBaseHealth;

    [Tooltip("기지가 피해를 한 번도 입지 않아야 달성(남은 체력 = 최대 체력)")]
    public bool noBaseDamage;

    [Tooltip("0이면 제한 없음. 전투 중 동시에 세워 둔 타워가 이 수 이하여야 달성")]
    [Min(0)] public int maxTowers;

    [Tooltip("이 타워(TowerData.id)를 한 번도 설치하지 않아야 달성")]
    public string[] bannedTowerIds = System.Array.Empty<string>();

    [Tooltip("보상 스킬 포인트")]
    [Min(1)] public int rewardAmount = 1;

    /// <summary>한 전투의 결과·기록이 이 퀘스트 조건을 모두 만족하는지.</summary>
    public bool IsMetBy(LastOfTheTower.Progression.BattleResult result, BattleChallengeStats stats)
    {
        if (result == null || !result.IsVictory || result.StageId != targetStageId)
            return false;
        if (result.RemainingBaseHealth < minRemainingBaseHealth)
            return false;
        // 아래 조건은 전투 기록이 있어야 확인할 수 있다(없으면 달성 아님).
        if (noBaseDamage && (stats == null || stats.BaseDamaged))
            return false;
        if (maxTowers > 0 && (stats == null || stats.MaxTowers > maxTowers))
            return false;
        if (bannedTowerIds != null && bannedTowerIds.Length > 0)
        {
            if (stats == null)
                return false;
            foreach (string id in bannedTowerIds)
                if (!string.IsNullOrEmpty(id) && stats.HasBuilt(id))
                    return false;
        }
        return true;
    }

    /// <summary>조건을 짧게 적은 문구(도전 과제 칸 등). 제목이 있으면 제목을 쓴다.</summary>
    public string ConditionText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(questTitle))
                return questTitle;
            var parts = new List<string>();
            if (noBaseDamage) parts.Add("기지 무피해");
            else if (minRemainingBaseHealth > 0) parts.Add($"기지 체력 {minRemainingBaseHealth} 이상");
            if (maxTowers > 0) parts.Add($"타워 {maxTowers}개 이하");
            if (bannedTowerIds != null && bannedTowerIds.Length > 0) parts.Add("지정 타워 없이");
            return parts.Count > 0 ? string.Join(" · ", parts) : "클리어";
        }
    }
}
