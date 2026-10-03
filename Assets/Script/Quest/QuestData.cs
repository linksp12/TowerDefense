using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "TowerDefense/Quest Data")]
public class QuestData : ScriptableObject
{
    public string questID;

    public enum QuestType
    {
        StageClear,
        MonsterHunt,
        UseGold
    }

    public QuestType questType;
    [Header("Type: StageClear")]
    public int targetStageNumber;
    [Header("Type: MonsterHunt")]
    public string targetMonsterID;
    public int targetMonsterCount;
    [Header("Type: UseGold")]
    public int targetGoldAmount;

    public string questDescription;
    
    [System.Serializable]
    public struct QuestReward
    {
        public string rewardItemID;
        public int rewardAmount;
    }

    public List<QuestReward> rewards;
}