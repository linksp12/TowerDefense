using UnityEngine;

public class QuestButton : MonoBehaviour
{
    [SerializeField] private string questId = "1";
    [SerializeField] private string stageId = "1";
    [SerializeField] private QuestManager questManager;

    public void OnClickAcceptButton()
    {
        if (questManager != null)
        {
            questManager.AcceptQuest(questId);
        }
    }

    public void OnClickVictoryButton()
    {
        if (questManager != null)
        {
            questManager.OnStageClear(stageId);
        }
    }
}