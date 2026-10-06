using UnityEngine;

public class QuestButton : MonoBehaviour //해당 파일은 임시라서 삭제 가능하다.
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