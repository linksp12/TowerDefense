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
        QuestData quest = FindQuest(questId);
        if (!CanSave || quest == null || GetState(questId) != QuestState.None)
            return false;

        // 대상 스테이지가 잠겨 있으면 수락할 수 없다.
        var progress = new StageProgressService(new PlayerPrefsStageProgressStorage());
        progress.Load();
        if (!StageIds.IsUnlocked(progress, quest.targetStageId))
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

    /// <summary>방금 끝난 전투의 도전 과제 결과(결과창 표시용, 저장하지 않음).</summary>
    public sealed class ChallengeReport
    {
        public string StageId;
        public readonly List<QuestData> Challenges = new List<QuestData>();
        public readonly List<bool> AchievedNow = new List<bool>();
        public readonly List<bool> NewlyEarned = new List<bool>();
        public int PointsGained;
    }

    public ChallengeReport LastChallengeReport { get; private set; }

    public void OnBattleResult(BattleResult result) => OnBattleResult(result, null);

    // 한 전투의 확정 결과를 한 번 전달받는다.
    // 게시판 퀘스트: 수락한 것만, 조건을 모두 만족하면 달성(보상은 게시판에서 받기).
    // 스테이지 도전 과제: 수락 없이, 처음 만족하면 바로 별과 보상 포인트.
    public void OnBattleResult(BattleResult result, BattleChallengeStats stats)
    {
        if (result == null)
            return;

        var report = new ChallengeReport { StageId = result.StageId };
        foreach (QuestData challenge in ChallengesFor(result.StageId))
        {
            bool met = challenge.IsMetBy(result, stats);
            report.Challenges.Add(challenge);
            report.AchievedNow.Add(met);
            report.NewlyEarned.Add(false);
        }
        LastChallengeReport = report;

        if (!CanSave || !result.IsVictory)
            return;

        var completed = new List<string>();
        var earned = new List<string>();
        int points = 0;
        foreach (QuestData data in allQuestDatas)
        {
            if (data == null || !data.IsMetBy(result, stats))
                continue;

            QuestState state = GetState(data.questId);
            if (data.kind == QuestData.QuestKind.StageChallenge)
            {
                if (state == QuestState.Rewarded)
                    continue;
                states[data.questId] = QuestState.Rewarded;
                earned.Add(data.questId);
                points += data.rewardAmount;
            }
            else if (state == QuestState.InProgress)
            {
                states[data.questId] = QuestState.Completed;
                completed.Add(data.questId);
            }
        }

        if (completed.Count == 0 && earned.Count == 0)
            return;

        // 별 기록과 포인트를 같은 PlayerPrefs.Save로 함께 기록해 중복 지급을 막는다.
        bool saved = earned.Count > 0
            ? WriteStates(false) && ResearchProgressStore.AddSkillPoints(points)
            : WriteStates(true);
        if (!saved)
        {
            foreach (string questId in completed)
                states[questId] = QuestState.InProgress;
            foreach (string questId in earned)
                states.Remove(questId);
            WriteStates(true);
            return;
        }

        for (int i = 0; i < report.Challenges.Count; i++)
            report.NewlyEarned[i] = earned.Contains(report.Challenges[i].questId);
        report.PointsGained = points;
        StatesChanged?.Invoke();
    }

    /// <summary>스테이지의 도전 과제(별 순서대로).</summary>
    public List<QuestData> ChallengesFor(string stageId)
    {
        var list = new List<QuestData>();
        if (allQuestDatas == null)
            return list;
        foreach (QuestData data in allQuestDatas)
            if (data != null && data.kind == QuestData.QuestKind.StageChallenge && data.targetStageId == stageId)
                list.Add(data);
        list.Sort((a, b) => a.star.CompareTo(b.star));
        return list;
    }

    public bool IsChallengeEarned(QuestData challenge) =>
        challenge != null && GetState(challenge.questId) == QuestState.Rewarded;

    public int EarnedStars(string stageId)
    {
        int stars = 0;
        foreach (QuestData challenge in ChallengesFor(stageId))
            if (IsChallengeEarned(challenge))
                stars++;
        return stars;
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

        if (!TryReadSave(PlayerPrefs.GetString(SaveKey), out QuestSaveData save, out string error))
        {
            Debug.LogError(error, this);
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

    internal static bool TryReadSave(string json, out QuestSaveData save, out string error)
    {
        save = null;
        error = null;
        if (json == null) { save = new QuestSaveData(); return true; }
        try
        {
            var root = Newtonsoft.Json.Linq.JObject.Parse(json, new Newtonsoft.Json.Linq.JsonLoadSettings
            { DuplicatePropertyNameHandling = Newtonsoft.Json.Linq.DuplicatePropertyNameHandling.Error });
            if (root["version"]?.Type != Newtonsoft.Json.Linq.JTokenType.Integer ||
                (int)root["version"] != SaveVersion || root["quests"] is not Newtonsoft.Json.Linq.JArray quests)
                throw new FormatException("퀘스트 저장 형식이 올바르지 않습니다.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var quest in quests)
            {
                if (quest["questId"]?.Type != Newtonsoft.Json.Linq.JTokenType.String ||
                    quest["state"]?.Type != Newtonsoft.Json.Linq.JTokenType.Integer ||
                    string.IsNullOrWhiteSpace((string)quest["questId"]) || !ids.Add((string)quest["questId"]) ||
                    !Enum.IsDefined(typeof(QuestState), (int)quest["state"]))
                    throw new FormatException("퀘스트 항목이 올바르지 않습니다.");
            }
            save = JsonUtility.FromJson<QuestSaveData>(json);
            return true;
        }
        catch (Exception exception)
        {
            error = "퀘스트 기록을 읽을 수 없습니다: " + exception.Message;
            return false;
        }
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
