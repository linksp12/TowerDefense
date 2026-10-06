using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [SerializeField] private QuestData[] allQuestDatas;
    public QuestSaveData saveData = new QuestSaveData();

    public void AcceptQuest(string questId) //TODO: 퀘스트 수락 버튼과 연결, 매개변수: 퀘스트 ID
    {
        if (!saveData.questStates.ContainsKey(questId))
        {
            saveData.questStates[questId] = QuestState.InProgress;
        }
    }

    public void OnStageClear(string targetStageId) //TODO: 퀘스트 클리어 로직과 연결, 매개변수: 몇 스테이지인지 (ex: 1)
    {
        foreach (var data in allQuestDatas)
        {
            if (data != null && data.targetStageId == targetStageId)
            {
                string questId = data.questId;

                if (saveData.questStates.ContainsKey(questId) && saveData.questStates[questId] == QuestState.InProgress)
                {
                    saveData.questStates[questId] = QuestState.Completed;
                }
            }
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