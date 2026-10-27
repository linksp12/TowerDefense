using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string TowerIconObjectName = "TowerImage";

    [Header("Target Tower")]
    [SerializeField] private TowerAttack targetTowerPrefab;

    [Header("UI")]
    [SerializeField] private Image towerIconImage;

    private TowerData Data => targetTowerPrefab != null ? targetTowerPrefab.towerData : null;

    private void Awake()
    {
        FindTowerIconImage();
        ApplyTowerIcon();
    }

    private void FindTowerIconImage()
    {
        if (towerIconImage != null)
            return;

        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image.name == TowerIconObjectName)
            {
                towerIconImage = image;
                return;
            }
        }
    }

    private void ApplyTowerIcon()
    {
        if (towerIconImage == null)
        {
            Debug.LogWarning($"{name}: TowerImage UI를 찾지 못해 타워 아이콘을 표시할 수 없습니다.", this);
            return;
        }

        if (Data == null || Data.icon == null)
        {
            Debug.LogWarning($"{name}: TowerData 아이콘이 연결되지 않아 기존 카드 아이콘을 유지합니다.", this);
            return;
        }

        towerIconImage.sprite = Data.icon;
    }

    public string statInfo 
    {
        get 
        {
            if (targetTowerPrefab == null) return "정보 없음";

            return $"<color=#FFF379>공격력:</color> <color=white>{targetTowerPrefab.BaseDamage}</color>\n" +
                   $"<color=#FFF379>공격속도(s):</color> <color=white>{targetTowerPrefab.BaseAttackCooldown}</color>\n" +
                   $"<color=#FFF379>사거리:</color> <color=white>{targetTowerPrefab.BaseAttackRange}</color>\n" +
                   $"<color=#FFF379>업그레이드:</color> <color=white>{targetTowerPrefab.UpgradeRouteSummary}</color>";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TowerInfoPanel.Instance == null)
            return;

        if (Data == null)
        {
            Debug.LogError($"{name}: TowerCardUI에 연결된 타워 프리팹 또는 TowerData가 없습니다.", this);
            TowerInfoPanel.Instance.Show("정보 없음", "TowerData가 연결되지 않았습니다.", "정보 없음");
            return;
        }

        TowerInfoPanel.Instance.Show(Data.towerName, Data.description, statInfo);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TowerInfoPanel.Instance != null)
        {
            TowerInfoPanel.Instance.Hide();
        }
    }
}
