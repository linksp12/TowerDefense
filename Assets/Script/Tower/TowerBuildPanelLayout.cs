using UnityEngine;

/// <summary>설치 목록과 설명을 한 묶음으로 화면 안에 배치한다. 구매 로직은 담당하지 않는다.</summary>
public sealed class TowerBuildPanelLayout : MonoBehaviour
{
    [SerializeField] private RectTransform selectionPanel;
    [SerializeField] private RectTransform detailsPanel;
    [SerializeField] private PanelAnimator panelAnimator;
    [SerializeField, Min(0)] private float screenMargin = 12f;
    [SerializeField, Min(0)] private float panelGap = 12f;

    private Canvas canvas;
    private Vector3 buildPosition;
    private Vector2 positionOffset;
    private Vector2 lastCanvasSize;
    private bool hasPosition;

    public void Place(Vector3 worldPosition, Vector2 offset)
    {
        buildPosition = worldPosition;
        positionOffset = offset;
        hasPosition = true;
        RefreshLayout();
    }

    private void LateUpdate()
    {
        if (!hasPosition || selectionPanel == null || !selectionPanel.gameObject.activeInHierarchy || canvas == null)
            return;

        if (((RectTransform)canvas.transform).rect.size != lastCanvasSize)
            RefreshLayout();
    }

    private void RefreshLayout()
    {
        if (selectionPanel == null || detailsPanel == null || Camera.main == null)
            return;

        if (canvas == null) canvas = selectionPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var canvasRect = (RectTransform)canvas.transform;
        Rect bounds = canvasRect.rect;
        lastCanvasSize = bounds.size;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
            Camera.main.WorldToScreenPoint(buildPosition), uiCamera, out Vector2 position);
        position += positionOffset;

        // 설명을 열기 전부터 자리를 확보하므로 호버할 때 목록이 움직이지 않는다.
        Vector2 menuSize = selectionPanel.rect.size;
        Vector2 infoSize = detailsPanel.rect.size;
        float scale = Mathf.Min(1f,
            Mathf.Max(1f, bounds.width - screenMargin * 2) / (menuSize.x + panelGap + infoSize.x),
            Mathf.Max(1f, bounds.height - screenMargin * 2) / Mathf.Max(menuSize.y, infoSize.y));
        bool infoOnRight = position.x <= bounds.center.x;
        float reservedWidth = (infoSize.x + panelGap) * scale;
        float halfWidth = menuSize.x * scale * 0.5f;
        float halfHeight = Mathf.Max(menuSize.y, infoSize.y) * scale * 0.5f;
        position.x = Mathf.Clamp(position.x,
            bounds.xMin + screenMargin + halfWidth + (infoOnRight ? 0 : reservedWidth),
            bounds.xMax - screenMargin - halfWidth - (infoOnRight ? reservedWidth : 0));
        position.y = Mathf.Clamp(position.y,
            bounds.yMin + screenMargin + halfHeight, bounds.yMax - screenMargin - halfHeight);

        selectionPanel.position = canvasRect.TransformPoint(position);
        detailsPanel.anchoredPosition = new Vector2(
            (infoOnRight ? 1 : -1) * (menuSize.x * 0.5f + panelGap + infoSize.x * 0.5f), 0);
        if (panelAnimator != null)
        {
            panelAnimator.endScale = new Vector3(scale, scale, 1);
            panelAnimator.startScale = new Vector3(scale * 0.85f, scale * 0.85f, 1);
        }
        selectionPanel.localScale = new Vector3(scale, scale, 1);
    }
}
