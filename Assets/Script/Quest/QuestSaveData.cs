using System.Collections.Generic;

[System.Serializable]
public class QuestSaveData
{
    // string은 퀘스트 ID, QuestState는 퀘스트 상태
    public Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>();
}

public enum QuestState
{
    None,
    InProgress,
    Completed,
    Rewarded
}