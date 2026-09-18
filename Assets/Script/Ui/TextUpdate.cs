using UnityEngine;
using TMPro;

public class TextUpdate : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI text;

    [Header("Target Tower")]
    [SerializeField] private TowerAttack targetTowerPrefab;

    void Start()
    {
        if (text == null)
        {
            text = GetComponent<TextMeshProUGUI>();
        }

        if (targetTowerPrefab != null)
        {
            text.text = $"{targetTowerPrefab.cost} 원";
        }
    }
}