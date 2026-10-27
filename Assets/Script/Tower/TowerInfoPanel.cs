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
    [SerializeField] private RectTransform contentRect;
    [SerializeField, Min(1)] private float bodyFontSize = 14f;
    [SerializeField, Min(1)] private float minimumBodyFontSize = 12f;

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

        FitBodyContent();

        // 스크롤 목록보다 나중에 그려지되, 입력은 계속 카드로 전달한다.
        if (panel != null) panel.transform.SetAsLastSibling();
    }

    private void FitBodyContent()
    {
        if (contentRect == null || descText == null || statText == null) return;

        // 장식 이미지의 안쪽 영역을 기준으로 설명과 수치를 순서대로 배치한다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        float width = contentRect.rect.width;
        float height = contentRect.rect.height;
        var layout = contentRect.GetComponent<VerticalLayoutGroup>();
        float gap = layout != null ? layout.spacing : 0;
        float descHeight = 0, statHeight = 0;
        descText.enableAutoSizing = statText.enableAutoSizing = false;
        float minimum = Mathf.Min(bodyFontSize, minimumBodyFontSize);
        for (float size = bodyFontSize; ; size = Mathf.Max(minimum, size - 0.5f))
        {
            descText.fontSize = statText.fontSize = size;
            descHeight = descText.GetPreferredValues(descText.text, width, Mathf.Infinity).y;
            statHeight = statText.GetPreferredValues(statText.text, width, Mathf.Infinity).y;
            if (descHeight + statHeight + gap <= height || size <= minimum) break;
        }

        // 긴 문구가 추가되어도 수치 영역을 밀어내거나 테두리를 넘지 않는다.
        statHeight = Mathf.Min(statHeight, height);
        descHeight = Mathf.Min(descHeight, Mathf.Max(0, height - statHeight - gap));
        var descLayout = descText.GetComponent<LayoutElement>();
        var statLayout = statText.GetComponent<LayoutElement>();
        if (descLayout != null) descLayout.preferredHeight = descHeight;
        if (statLayout != null) statLayout.preferredHeight = statHeight;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
