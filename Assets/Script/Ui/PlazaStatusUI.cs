using LastOfTheTower.Progression;
using TMPro;
using UnityEngine;

// 광장 안내는 기존 진행/퀘스트 기록을 조회한다. 수락·보상·저장은 하지 않는다.
public sealed class PlazaStatusUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goalTitle;
    [SerializeField] private TMP_Text goalDescription;
    [SerializeField] private TMP_Text goalState;
    [SerializeField] private TMP_Text stageStatus;
    [SerializeField] private TMP_Text skillPoints;
    [SerializeField] private GameObject questRewardAlert;
    [SerializeField] private string[] stageIds =
        { StageIds.StageOne, StageIds.StageTwo, StageIds.StageThree, StageIds.StageFour };

    private QuestManager subscribedQuest;
    private bool started;

    private void Start()
    {
        started = true;
        BindAndRefresh();
    }

    private void OnEnable() { if (started) BindAndRefresh(); }

    private void OnDisable()
    {
        if (subscribedQuest != null) subscribedQuest.StatesChanged -= RefreshStatus;
        subscribedQuest = null;
    }

    private void BindAndRefresh()
    {
        if (subscribedQuest != null) subscribedQuest.StatesChanged -= RefreshStatus;
        subscribedQuest = QuestManager.Instance;
        if (subscribedQuest != null) subscribedQuest.StatesChanged += RefreshStatus;
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        var progress = new StageProgressService(new PlayerPrefsStageProgressStorage());
        progress.Load();
        int cleared = 0;
        int nextStage = -1;
        for (int i = 0; i < stageIds.Length; i++)
            if (progress.IsCleared(stageIds[i])) cleared++;
            else if (nextStage < 0) nextStage = i;
        stageStatus.text = progress.CanWrite
            ? $"클리어\n<color=#E2BB70>{cleared}/{stageIds.Length}</color>"
            : "클리어\n확인 필요";
        skillPoints.text = $"보유 SP\n<color=#E2BB70>{ResearchProgressStore.SkillPoints}</color>";

        QuestData completed = null, active = null, available = null;
        int rewards = 0;
        if (subscribedQuest != null && subscribedQuest.CanSave && subscribedQuest.Quests != null)
            foreach (QuestData quest in subscribedQuest.Quests)
            {
                if (quest == null) continue;
                switch (subscribedQuest.GetState(quest.questId))
                {
                    case QuestState.Completed: rewards++; if (completed == null) completed = quest; break;
                    case QuestState.InProgress: if (active == null) active = quest; break;
                    case QuestState.None: if (available == null) available = quest; break;
                }
            }
        questRewardAlert.SetActive(rewards > 0);
        goalState.color = rewards > 0 ? new Color32(154, 220, 160, 255) : new Color32(199, 174, 123, 255);

        if (subscribedQuest == null || !subscribedQuest.CanSave)
        {
            SetGoal("기록 확인 필요", "퀘스트 정보를 읽을 수 없습니다.", "확인 필요");
        }
        else if (completed != null)
        {
            SetGoal(completed.questTitle, "목표 달성!\n퀘스트 NPC에게 보상을 받으세요.", $"보상 {rewards}");
        }
        else if (active != null)
        {
            SetGoal(active.questTitle, active.questDescription, "진행 중");
        }
        else if (available != null)
        {
            SetGoal(available.questTitle, "퀘스트 NPC에게 말을 걸어\n새 의뢰를 수락하세요.", "새 의뢰");
        }
        else if (!progress.CanWrite)
        {
            SetGoal("기록 확인 필요", "스테이지 정보를 읽을 수 없습니다.", "확인 필요");
        }
        else if (nextStage >= 0)
        {
            SetGoal($"{nextStage + 1}스테이지에 도전하세요", "출전 NPC에게 말을 걸어\n다음 전투를 준비하세요.", "출전 준비");
        }
        else
        {
            SetGoal("모든 전장 클리어", "모든 스테이지를 클리어했습니다!\n원하는 전장에 다시 도전하세요.", "완료");
        }
    }

    private void SetGoal(string title, string description, string state)
    {
        goalTitle.text = title;
        goalDescription.text = description;
        goalState.text = state;
    }
}
