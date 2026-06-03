using UnityEngine;
using TMPro;

public class TowerInfoPanel : MonoBehaviour
{
    public static TowerInfoPanel Instance;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI statText;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    public void Show(string towerName, string description, string statInfo)
    {
        if (panel != null)
            panel.SetActive(true);

        if (nameText != null)
            nameText.text = towerName;

        if (descText != null)
            descText.text = description;

        if (statText != null)
            statText.text = statInfo;
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
