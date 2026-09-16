using UnityEngine;
using UnityEngine.EventSystems;

public class TowerCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target Tower")]
    [SerializeField] private TowerAttack targetTowerPrefab;
    
    [Header("Tower Info")]
    public string towerName;

    [TextArea]
    public string description;

    public string statInfo 
    {
        get 
        {
            if (targetTowerPrefab == null) return "정보 없음";

            return $"<color=#FFF379>공격력:</color> <color=white>{targetTowerPrefab.damage}</color>\n" +
                   $"<color=#FFF379>공격속도(s):</color> <color=white>{targetTowerPrefab.attackCooldown}</color>\n" +
                   $"<color=#FFF379>사거리:</color> <color=white>{targetTowerPrefab.attackRange}</color>\n" +
                   $"<color=#FFF379>업그레이드:</color> <color=white>{targetTowerPrefab.upgrade}</color>";
        }
    }

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
