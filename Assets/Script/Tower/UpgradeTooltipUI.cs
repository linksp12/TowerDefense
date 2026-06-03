using UnityEngine;
using TMPro;

public class UpgradeTooltipUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;

    private void Awake()
    {
        Hide();
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
