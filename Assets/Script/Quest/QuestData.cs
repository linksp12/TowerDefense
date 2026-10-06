using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestData", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string questDescription;
    public string targetStageId;
    public int rewardAmount;
}