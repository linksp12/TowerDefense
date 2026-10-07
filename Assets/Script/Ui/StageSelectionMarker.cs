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
    [SerializeField] private bool previewOnly;

    private bool hovered;
    private bool progressKnown = true;
    private bool cleared;
    private bool unlocked = true;
    private string lockMessage = "잠김";
    private StageSelectionDetailsUI details;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        if (detailsPanel != null)
            details = detailsPanel.GetComponent<StageSelectionDetailsUI>();
    }

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
        if (!unlocked || !button.IsInteractable())
            return;
        IsSelected = true;
        Refresh();
        if (details != null)
        {
            detailsPanel.SetActive(true);
            details.Show();
        }
    }

    public void CloseDetails()
    {
        if (details != null && !details.CanClose)
            return;
        // 상세창을 닫으면 선택도 풀어 지도 표시를 기본 상태로 되돌린다.
        IsSelected = false;
        hovered = false;
        Refresh();
        if (details != null)
            details.Hide();
    }

    public void SetProgressState(bool known, bool isCleared)
    {
        progressKnown = known;
        cleared = known && isCleared;
        Refresh();
    }

    public void SetAvailability(bool isUnlocked, string message)
    {
        unlocked = isUnlocked;
        lockMessage = message;
        button.interactable = unlocked;
        if (!unlocked)
        {
            hovered = false;
            IsSelected = false;
        }
        Refresh();
    }

    private void Refresh()
    {
        bool highlighted = unlocked && (hovered || IsSelected);
        frame.color = highlighted
            ? new Color32(255, 217, 126, 255)
            : unlocked ? new Color32(163, 121, 57, 255) : new Color32(89, 95, 100, 255);
        surface.color = highlighted
            ? new Color32(29, 45, 58, 250)
            : new Color32(14, 26, 37, 245);
        statusText.text = !unlocked ? lockMessage : IsSelected ? "선택됨" : hovered ? "클릭하여 선택" :
            previewOnly ? "상세 보기" : !progressKnown ? "기록 확인 불가" :
            cleared ? "다시 도전 가능" : "도전 가능";
        statusText.color = highlighted
            ? new Color32(255, 221, 151, 255)
            : new Color32(177, 189, 181, 255);
        selectionAccent.SetActive(IsSelected);
    }
}
