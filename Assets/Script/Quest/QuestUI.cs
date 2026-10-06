using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private GameObject questListObject;
    [SerializeField] private Transform questSlotParent;
    [SerializeField] private GameObject questSlotPrefab;

    public void ToggleQuestList()
    {
        if (questListObject != null)
        {
            bool isActive = questListObject.activeSelf;
            questListObject.SetActive(!isActive);

            if (!isActive)
            {
                RefreshQuestList();
            }
            else
            {
                ClearQuestList();
            }
        }
    }

    private void RefreshQuestList()
    {
        ClearQuestList();

        QuestManager manager = FindFirstObjectByType<QuestManager>();
        if (manager != null && questSlotPrefab != null && questSlotParent != null)
        {
            foreach (var kvp in manager.saveData.questStates)
            {
                // Rewarded 상태가 아닌 퀘스트만 동적 생성
                if (kvp.Value != QuestState.Rewarded)
                {
                    GameObject slotObj = Instantiate(questSlotPrefab, questSlotParent);
                    QuestSlotUI slotUI = slotObj.GetComponent<QuestSlotUI>();
                    if (slotUI != null)
                    {
                        string description = manager.GetQuestDescription(kvp.Key);
                        slotUI.Setup(description, kvp.Value);
                    }
                }
            }
        }
    }

    private void ClearQuestList()
    {
        if (questSlotParent != null)
        {
            foreach (Transform child in questSlotParent)
            {
                Destroy(child.gameObject);
            }
        }
    }
}