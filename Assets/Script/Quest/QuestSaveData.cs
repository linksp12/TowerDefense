using UnityEngine;
using System;

[Serializable]
public class QuestSaveData
{
    public string questId;

    public enum QuestState
    {
        InProgress,
        Cleared,
    }

    public QuestState questState;

    public QuestSaveData(string id, QuestState state)
    {
        questId = id;
        questState = state;
    }
}