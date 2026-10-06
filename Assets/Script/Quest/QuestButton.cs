using UnityEngine;

public class QuestButton : MonoBehaviour
{
    [SerializeField] private string questId = "1";
    [SerializeField] private QuestManager questManager;

    public void OnClickAcceptButton()
    {
        if (questManager != null)
        {
            questManager.AcceptQuest(questId);
        }
    }
}