using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button rewardButton;
    private string currentQuestId;

    public void Setup(string questId, string description, QuestState state)
    {
        currentQuestId = questId;

        if (descriptionText != null && buttonText != null)
        {
            descriptionText.text = $"{description}";
            buttonText.text = $"{state}";
        }

        if (rewardButton != null)
        {
            rewardButton.gameObject.SetActive(state == QuestState.Completed);
        }
    }

    public void OnClickRewardButton()
    {
        QuestManager manager = FindFirstObjectByType<QuestManager>();
        if (manager != null)
        {
            manager.ClaimReward(currentQuestId);
        }
    }
}