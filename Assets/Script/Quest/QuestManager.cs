using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    [SerializeField] private List<QuestData> questDatabase = new List<QuestData>();
    [SerializeField] private List<QuestSaveData> activeQuests = new List<QuestSaveData>();
    [SerializeField] private List<String> clearedQuestIds = new List<String>();
    
    public void QuestAccept(string questId) //중복 수락 로직은 여기서 안할거라 생략
    {
        QuestSaveData newSaveData = new QuestSaveData(questId, QuestSaveData.QuestState.InProgress);
        activeQuests.Add(newSaveData);
    }

    public List GetAllQuest()
    {
        List result = new List();
        
        foreach (var questSave in activeQuests)
        {
            if (questSave.questState == QuestSaveData.QuestState.InProgress)
            {
                QuestData data = GetQuestData(questSave.questId);
                if (data != null)
                {
                    result.Add(data);
                }
            }
        }
        
        return result;
    }

    public QuestData GetOneQuest(string id) //퀘스트 아이콘이 아닐때, 특정 퀘스트의 정보를 반환
    {
        return questDatabase.Find(q => q.questID == id);
    }

    public void QuestCheck_StageClear(int stageNumber) //외부에서 스테이지 클리어 시 호출할 함수
    {
        foreach (var quest in activeQuests)
        {
            if (quest.questState == QuestSaveData.QuestState.InProgress)
            {
                QuestData questData = QuestLoad(quest.questId);
                
                if (questData != null && questData.questType == QuestData.QuestType.StageClear && questData.targetStageNumber == stageNumber)
                {
                    quest.questState = QuestState.Cleared;
                }
            }
        }
    }

    public void QuestCheck_MonsterHunt()
    {
        //나중에 구현
    }

    public void QuestCheck_UseGold()
    {
        //나중에 구현
    }

    public void QuestClaimReward(string questId)
    {
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            var quest = activeQuests[i];
            if (quest.questId == questId)
            {
                if (quest.questState == QuestSaveData.QuestState.Cleared)
                {
                    // TODO: 보상 지급 로직 실행 (questData.rewards 활용 필요)
                    clearedQuestIds.Add(questId);
                    activeQuests.RemoveAt(i); 
                }
                break;
            }
        }
    }
}
