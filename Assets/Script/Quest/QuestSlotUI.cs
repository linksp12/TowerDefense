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

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        if (rewardButton != null && buttonText != null)
        {
            if (state == QuestState.InProgress)
            {
                rewardButton.gameObject.SetActive(true);
                rewardButton.interactable = false; 
                buttonText.text = "진행중";
            }
            else if (state == QuestState.Completed)
            {
                rewardButton.gameObject.SetActive(true);
                rewardButton.interactable = true;
                buttonText.text = "보상받기";
            }
        }
    }

    public void OnClickRewardButton()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ClaimReward(currentQuestId);
        }
    }
}