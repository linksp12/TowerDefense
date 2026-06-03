using UnityEngine;
using UnityEngine.EventSystems;

public class TowerClick : MonoBehaviour
{
    private TowerUpgrade towerUpgrade;

    void Awake()
    {
        towerUpgrade = GetComponent<TowerUpgrade>();
    }

    void OnMouseDown()
    {
        // UI 버튼/패널 위를 클릭한 경우 타워 클릭 무시
        if (IsPointerOverUI())
        {
            return;
        }

        if (towerUpgrade == null)
        {
            Debug.LogWarning("TowerUpgrade가 없습니다.");
            return;
        }

        if (TowerUpgradeUI.Instance == null)
        {
            Debug.LogWarning("TowerUpgradeUI가 씬에 없습니다.");
            return;
        }

        TowerUpgradeUI.Instance.Open(towerUpgrade);
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
