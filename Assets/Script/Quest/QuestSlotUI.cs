using UnityEngine;
using TMPro;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI buttonText;

    public void Setup(string description, QuestState state)
    {
        if (descriptionText != null && buttonText != null)
        {
            descriptionText.text = $"{description}";
            buttonText.text = $"{state}";
        }
    }
}