using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [SerializeField] private QuestData[] allQuestDatas;
    public QuestSaveData saveData = new QuestSaveData();

    public void AcceptQuest(string questId)
    {
        if (!saveData.questStates.ContainsKey(questId))
        {
            saveData.questStates[questId] = QuestState.InProgress;
        }
    }

    public string GetQuestDescription(string questId)
    {
        foreach (var data in allQuestDatas)
        {
            if (data != null && data.questId == questId)
            {
                return data.questDescription;
            }
        }
        return "설명이 없습니다.";
    }
}