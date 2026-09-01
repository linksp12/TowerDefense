using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TooltipType
    {
        PathA,
        PathB,
        FinalUpgrade,
        Sell
    }

    public TooltipType tooltipType;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TowerUpgradeUI.Instance != null)
        {
            TowerUpgradeUI.Instance.ShowTooltip(tooltipType);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TowerUpgradeUI.Instance != null)
        {
            TowerUpgradeUI.Instance.HideTooltip();
        }
    }
}
