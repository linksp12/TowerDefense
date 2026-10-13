using System;
using System.Collections.Generic;
using LastOfTheTower.Progression;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    internal const string SaveKey = "quest.states";
    private const int SaveVersion = 1;

    public static QuestManager Instance { get; private set; }

    [SerializeField] private QuestData[] allQuestDatas;

    private readonly Dictionary<string, QuestState> states = new Dictionary<string, QuestState>(StringComparer.Ordinal);

    // 저장 데이터를 읽지 못했으면 기존 기록을 덮어쓰지 않도록 수락·보상을 막는다.
    public bool CanSave { get; private set; }
    public IReadOnlyList<QuestData> Quests => allQuestDatas;
    public event Action StatesChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public QuestState GetState(string questId)
    {
        return questId != null && states.TryGetValue(questId, out QuestState state) ? state : QuestState.None;
    }

    public bool AcceptQuest(string questId)
    {
        if (!CanSave || FindQuest(questId) == null || GetState(questId) != QuestState.None)
            return false;

        states[questId] = QuestState.InProgress;
        if (!WriteStates(true))
        {
            states.Remove(questId);
            return false;
        }

        StatesChanged?.Invoke();
        return true;
    }

    // 한 전투의 확정 결과를 한 번 전달받는다. 수락한 퀘스트만, 승리일 때만 달성으로 바꾼다.
    public void OnBattleResult(BattleResult result)
    {
        if (!CanSave || result == null || !result.IsVictory)
            return;

        var completed = new List<string>();
        foreach (QuestData data in allQuestDatas)
        {
            if (data == null || data.targetStageId != result.StageId ||
                GetState(data.questId) != QuestState.InProgress ||
                result.RemainingBaseHealth < data.minRemainingBaseHealth)
                continue;

            states[data.questId] = QuestState.Completed;
            completed.Add(data.questId);
        }

        if (completed.Count == 0)
            return;

        if (!WriteStates(true))
        {
            foreach (string questId in completed)
                states[questId] = QuestState.InProgress;
            return;
        }

        StatesChanged?.Invoke();
    }

    public bool ClaimReward(string questId)
    {
        QuestData data = FindQuest(questId);
        if (!CanSave || data == null || GetState(questId) != QuestState.Completed)
            return false;

        // 수령 기록과 스킬 포인트를 같은 PlayerPrefs.Save로 함께 기록해 중복 지급을 막는다.
        states[questId] = QuestState.Rewarded;
        if (!WriteStates(false) || !ResearchProgressStore.AddSkillPoints(data.rewardAmount))
        {
            states[questId] = QuestState.Completed;
            WriteStates(true);
            return false;
        }

        StatesChanged?.Invoke();
        return true;
    }

    public QuestData FindQuest(string questId)
    {
        if (allQuestDatas == null || string.IsNullOrEmpty(questId))
            return null;

        foreach (QuestData data in allQuestDatas)
        {
            if (data != null && data.questId == questId)
                return data;
        }
        return null;
    }

    internal void ReloadProgress()
    {
        Load();
        StatesChanged?.Invoke();
    }

    private void Load()
    {
        states.Clear();
        CanSave = false;

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            CanSave = true;
            return;
        }

        QuestSaveData save = null;
        try
        {
            save = JsonUtility.FromJson<QuestSaveData>(PlayerPrefs.GetString(SaveKey));
        }
        catch (Exception exception)
        {
            Debug.LogError($"퀘스트 기록을 읽지 못했습니다. 기존 기록을 보존합니다: {exception.Message}", this);
            return;
        }

        if (save == null || save.version != SaveVersion || save.quests == null)
        {
            Debug.LogError("퀘스트 기록 형식이 올바르지 않습니다. 기존 기록을 보존합니다.", this);
            return;
        }

        foreach (QuestStateEntry entry in save.quests)
        {
            if (entry == null || string.IsNullOrEmpty(entry.questId) ||
                !Enum.IsDefined(typeof(QuestState), entry.state) || states.ContainsKey(entry.questId))
            {
                Debug.LogError("퀘스트 기록에 잘못된 항목이 있습니다. 기존 기록을 보존합니다.", this);
                states.Clear();
                return;
            }
            states[entry.questId] = entry.state;
        }

        CanSave = true;
    }

    private bool WriteStates(bool flush)
    {
        var save = new QuestSaveData { version = SaveVersion };
        foreach (KeyValuePair<string, QuestState> pair in states)
        {
            if (pair.Value != QuestState.None)
                save.quests.Add(new QuestStateEntry { questId = pair.Key, state = pair.Value });
        }

        try
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            if (flush)
                PlayerPrefs.Save();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"퀘스트 기록 저장 실패: {exception.Message}", this);
            return false;
        }
    }
}
