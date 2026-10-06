using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private GameObject questListObject;

    public void ToggleQuestList()
    {
        if (questListObject != null)
        {
            bool isActive = questListObject.activeSelf;
            questListObject.SetActive(!isActive);
        }
    }
}