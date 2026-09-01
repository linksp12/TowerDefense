using UnityEngine;
using UnityEngine.EventSystems;

public class TowerCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tower Info")]
    public string towerName;

    [TextArea]
    public string description;

    [TextArea]
    public string statInfo;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TowerInfoPanel.Instance != null)
        {
            TowerInfoPanel.Instance.Show(towerName, description, statInfo);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TowerInfoPanel.Instance != null)
        {
            TowerInfoPanel.Instance.Hide();
        }
    }
}
