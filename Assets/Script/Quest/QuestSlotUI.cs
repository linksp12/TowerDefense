using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button rewardButton;
    [SerializeField] private Image buttonFill;
    [SerializeField] private Image stateAccent;
    [SerializeField] private CanvasGroup canvasGroup;

    private static readonly Color32 AcceptFill = new Color32(206, 154, 68, 255);
    private static readonly Color32 ClaimFill = new Color32(74, 168, 112, 255);
    private static readonly Color32 IdleFill = new Color32(70, 62, 50, 255);
    private static readonly Color32 DarkLabel = new Color32(30, 24, 18, 255);
    private static readonly Color32 LightLabel = new Color32(236, 226, 204, 255);
    private static readonly Color32 MutedLabel = new Color32(170, 160, 140, 255);

    private string currentQuestId;
    private QuestState currentState;

    public void Setup(QuestData data, QuestState state, bool canSave)
    {
        currentQuestId = data.questId;
        currentState = state;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(data.questTitle) ? data.questDescription : data.questTitle;
        if (descriptionText != null)
            descriptionText.text = data.questDescription;
        if (rewardText != null)
            rewardText.text = $"스킬 포인트 +{data.rewardAmount}";

        string label;
        Color32 fill;
        Color32 labelColor;
        Color32 accent;
        bool interactable;
        switch (state)
        {
            case QuestState.InProgress:
                label = "진행 중"; fill = IdleFill; labelColor = LightLabel; accent = new Color32(120, 196, 218, 255); interactable = false;
                break;
            case QuestState.Completed:
                label = "보상 받기"; fill = ClaimFill; labelColor = DarkLabel; accent = ClaimFill; interactable = true;
                break;
            case QuestState.Rewarded:
                label = "완료"; fill = IdleFill; labelColor = MutedLabel; accent = new Color32(110, 100, 84, 255); interactable = false;
                break;
            default:
                label = "수락"; fill = AcceptFill; labelColor = DarkLabel; accent = new Color32(201, 158, 82, 255); interactable = true;
                break;
        }

        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.color = labelColor;
        }
        if (buttonFill != null)
            buttonFill.color = fill;
        if (stateAccent != null)
            stateAccent.color = accent;
        if (rewardButton != null)
            rewardButton.interactable = interactable && canSave;
        if (canvasGroup != null)
            canvasGroup.alpha = state == QuestState.Rewarded ? 0.6f : 1f;
    }

    // 버튼 하나가 상태에 따라 수락 또는 보상 받기를 처리한다. (프리팹의 기존 연결 이름 유지)
    public void OnClickRewardButton()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null || rewardButton == null || !rewardButton.interactable)
            return;

        // 처리 중 연속 클릭을 막는다. 성공하면 게시판이 새 상태로 다시 그린다.
        rewardButton.interactable = false;
        bool handled = currentState == QuestState.None ? manager.AcceptQuest(currentQuestId) :
            currentState == QuestState.Completed && manager.ClaimReward(currentQuestId);

        if (handled && AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
        if (!handled)
            rewardButton.interactable = true;
    }
}
