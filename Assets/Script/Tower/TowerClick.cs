using UnityEngine;

public class TowerClick : MonoBehaviour
{
    private TowerUpgrade towerUpgrade;

    void Awake()
    {
        towerUpgrade = GetComponent<TowerUpgrade>();
    }

    void OnMouseDown()
    {
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
}
