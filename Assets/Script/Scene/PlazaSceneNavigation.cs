using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlazaSceneNavigation : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogue;
    [SerializeField] private string stageSelectionScenePath = "Assets/Scenes/StageSelectionScene.unity";
    [SerializeField] private string researchScenePath = "Assets/Scenes/ResearchScene.unity";
    [SerializeField] private string questScenePath = "Assets/Scenes/Quest.unity";

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
        if (!Application.isPlaying || IsTransitioning)
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
                scenePath = questScenePath;
                break;
            default:
                return;
        }

        if (string.IsNullOrWhiteSpace(scenePath) ||
            !Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError($"광장 이동 대상 씬을 빌드 씬 목록에서 확인하세요: {scenePath}", this);
            return;
        }

        float previousTimeScale = Time.timeScale;
        bool previousAudioPause = AudioListener.pause;
        IsTransitioning = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        try
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (operation == null)
                throw new System.InvalidOperationException("씬 로드 요청을 생성하지 못했습니다.");

            dialogue.CloseDialogue();
        }
        catch (System.Exception exception)
        {
            IsTransitioning = false;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            Debug.LogError($"광장 씬 이동 실패: {scenePath}\n{exception}", this);
        }
    }
}
