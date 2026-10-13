using System.Collections.Generic;

// JsonUtility로 저장하는 퀘스트 진행 기록. Dictionary는 직렬화되지 않으므로 목록으로 저장한다.
[System.Serializable]
public class QuestSaveData
{
    public int version = 1;
    public List<QuestStateEntry> quests = new List<QuestStateEntry>();
}

[System.Serializable]
public class QuestStateEntry
{
    public string questId;
    public QuestState state;
}

public enum QuestState
{
    None,
    InProgress,
    Completed,
    Rewarded
}
