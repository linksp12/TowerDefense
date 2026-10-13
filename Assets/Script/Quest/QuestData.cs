using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestData", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string questTitle;
    public string questDescription;
    public string targetStageId;
    [Tooltip("0이면 체력 조건 없음. 클리어 순간 남은 기지 체력이 이 값 이상이어야 달성")]
    [Min(0)] public int minRemainingBaseHealth;
    [Tooltip("보상 스킬 포인트")]
    [Min(1)] public int rewardAmount = 1;
}
