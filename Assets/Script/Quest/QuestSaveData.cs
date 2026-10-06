using System.Collections.Generic;

[System.Serializable]
public class QuestSaveData
{
    public Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>();
}

public enum QuestState
{
    None,
    InProgress,
    Completed,
    Rewarded
}