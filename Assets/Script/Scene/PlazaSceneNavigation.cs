using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlazaSceneNavigation : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogue;
    [SerializeField] private string stageSelectionScenePath = "Assets/Scenes/StageSelectionScene.unity";
    [SerializeField] private string researchScenePath = "Assets/Scenes/ResearchScene.unity";
    [SerializeField] private QuestUI questBoard;

    public bool IsTransitioning { get; private set; }

    private void OnEnable()
    {
        if (dialogue == null)
        {
            Debug.LogError("광장 씬 이동에 DialogueUI 연결이 필요합니다.", this);
            return;
        }

        dialogue.ActionRequested += OnActionRequested;
    }

    private void OnDisable()
    {
        if (dialogue != null)
            dialogue.ActionRequested -= OnActionRequested;
    }

    private void OnActionRequested(PlazaNPC.NPCType npcType)
    {
        if (!Application.isPlaying || IsTransitioning || SceneLoadingScreen.IsLoading)
            return;

        string scenePath;
        switch (npcType)
        {
            case PlazaNPC.NPCType.Stage:
                scenePath = stageSelectionScenePath;
                break;
            case PlazaNPC.NPCType.Skill:
                scenePath = researchScenePath;
                break;
            case PlazaNPC.NPCType.Quest:
                // 퀘스트 받기: 대화를 닫고 게시판에서 원하는 퀘스트를 고르게 한다.
                dialogue.CloseDialogue();
                if (questBoard != null)
                    questBoard.Open();
                else
                    Debug.LogError("광장 퀘스트 게시판 연결이 필요합니다.", this);
                return;
            default:
                return;
        }

        if (string.IsNullOrWhiteSpace(scenePath) ||
            !Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError($"광장 이동 대상 씬을 빌드 씬 목록에서 확인하세요: {scenePath}", this);
            return;
        }

        IsTransitioning = true;
        string message = npcType == PlazaNPC.NPCType.Stage
            ? "스테이지 선택 화면으로 이동 중"
            : "업그레이드 화면으로 이동 중";
        if (SceneLoadingScreen.TryLoad(scenePath, message, RestoreAfterLoadFailure))
            dialogue.CloseDialogue();
        else
            RestoreAfterLoadFailure();
    }

    private void RestoreAfterLoadFailure()
    {
        if (this != null)
            IsTransitioning = false;
    }
}
