using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeTooltipUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;

    private void Awake()
    {
        DisableRaycastTargets();
        Hide();
    }

    private void DisableRaycastTargets()
    {
        if (panel == null)
            return;

        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }

    public void Show(string title, string desc)
    {
        if (panel != null)
            panel.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (descText != null)
            descText.text = desc;
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
