using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectionMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image frame;
    [SerializeField] private Image surface;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject selectionAccent;
    [SerializeField] private GameObject detailsPanel;

    private bool hovered;
    public bool IsSelected { get; private set; }

    private void OnEnable()
    {
        button.onClick.AddListener(Select);
        Refresh();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(Select);
        hovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Refresh();
    }

    private void Select()
    {
        IsSelected = true;
        Refresh();
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
            detailsPanel.GetComponent<StageSelectionDetailsUI>().Show();
        }
    }

    public void CloseDetails()
    {
        Refresh();
        if (detailsPanel != null)
            detailsPanel.GetComponent<StageSelectionDetailsUI>().Hide();
    }

    private void Refresh()
    {
        bool highlighted = hovered || IsSelected;
        frame.color = highlighted
            ? new Color32(255, 217, 126, 255)
            : new Color32(163, 121, 57, 255);
        surface.color = highlighted
            ? new Color32(29, 45, 58, 250)
            : new Color32(14, 26, 37, 245);
        statusText.text = IsSelected ? "선택됨" : hovered ? "클릭하여 선택" : "도전 가능";
        statusText.color = highlighted
            ? new Color32(255, 221, 151, 255)
            : new Color32(177, 189, 181, 255);
        selectionAccent.SetActive(IsSelected);
    }
}
