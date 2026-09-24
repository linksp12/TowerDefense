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

    private TowerData Data => targetTowerPrefab != null ? targetTowerPrefab.towerData : null;

    public string statInfo 
    {
        get 
        {
            if (targetTowerPrefab == null) return "정보 없음";

            return $"<color=#FFF379>공격력:</color> <color=white>{targetTowerPrefab.BaseDamage}</color>\n" +
                   $"<color=#FFF379>공격속도(s):</color> <color=white>{targetTowerPrefab.BaseAttackCooldown}</color>\n" +
                   $"<color=#FFF379>사거리:</color> <color=white>{targetTowerPrefab.BaseAttackRange}</color>\n" +
                   $"<color=#FFF379>업그레이드:</color> <color=white>{targetTowerPrefab.upgrade}</color>";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TowerInfoPanel.Instance != null)
        {
            string displayName = Data != null && !string.IsNullOrWhiteSpace(Data.towerName)
                ? Data.towerName
                : towerName;

            string displayDescription = Data != null && !string.IsNullOrWhiteSpace(Data.description)
                ? Data.description
                : description;

            TowerInfoPanel.Instance.Show(displayName, displayDescription, statInfo);
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
