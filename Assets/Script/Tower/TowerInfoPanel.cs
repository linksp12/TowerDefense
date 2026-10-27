using UnityEngine;
using UnityEngine.UI;
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

        DisableRaycastTargets();

        if (panel != null)
            panel.SetActive(false);
    }

    private void DisableRaycastTargets()
    {
        if (panel == null)
            return;

        // 정보 패널은 표시 전용이다. 마우스 입력을 받으면 카드의
        // PointerExit를 발생시켜 패널이 빠르게 켜졌다 꺼질 수 있다.
        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
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
