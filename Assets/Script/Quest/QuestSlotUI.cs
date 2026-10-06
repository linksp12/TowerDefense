using UnityEngine;
using TMPro;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void Setup(string description, QuestState state)
    {
        if (descriptionText != null)
        {
            descriptionText.text = $"{description}";
        }
    }
}