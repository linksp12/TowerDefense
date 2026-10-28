using UnityEngine;

/// <summary>
/// 설치·강화 창이 지도 위에 보여 주는 사거리 원, 설치 미리보기(반투명 타워), 설치 자리 마법진.
/// 창마다 하나씩 만들어 쓰고, 창이 닫히면 숨긴다.
/// </summary>
public sealed class TowerRangePreview : MonoBehaviour
{
    private SpriteRenderer range;
    private SpriteRenderer rune;
    private SpriteRenderer ghost;
    private Color rangeColor = Color.white;

    public static TowerRangePreview Create(string name)
    {
        var preview = new GameObject(name).AddComponent<TowerRangePreview>();
        // 땅 위·몬스터 아래에 원과 마법진, 타워 층에 미리보기
        preview.range = preview.MakeRenderer("Range", "range_ring", "GroundProps", 200);
        preview.rune = preview.MakeRenderer("Rune", "rune", "GroundProps", 201);
        preview.ghost = preview.MakeRenderer("Ghost", null, "Towers", 50);
        preview.gameObject.SetActive(false);
        return preview;
    }

    private SpriteRenderer MakeRenderer(string childName, string sprite, string layer, int order)
    {
        var child = new GameObject(childName).AddComponent<SpriteRenderer>();
        child.transform.SetParent(transform, false);
        if (sprite != null)
            child.sprite = TowerPanelArt.Sprite(sprite);
        child.sortingLayerName = layer;
        child.sortingOrder = order;
        return child;
    }

    public void Show(Vector3 position, float radius, Color color, Sprite ghostSprite, Vector3 ghostScale, bool showRune)
    {
        transform.position = position;
        rangeColor = color;
        range.enabled = radius > 0f;
        range.color = color;
        // 원 그림은 지름 2칸이라 반지름만큼 키운다.
        range.transform.localScale = new Vector3(radius, radius, 1f);
        ghost.enabled = ghostSprite != null;
        ghost.sprite = ghostSprite;
        ghost.transform.localScale = ghostScale == Vector3.zero ? Vector3.one : ghostScale;
        rune.enabled = showRune;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        if (ghost.enabled)
            ghost.color = new Color(1f, 1f, 1f, 0.5f + 0.12f * Mathf.Sin(t * 4f));
        if (rune.enabled)
            rune.color = new Color(1f, 1f, 1f, 0.65f + 0.25f * Mathf.Sin(t * 3f));
        if (range.enabled)
            range.color = new Color(rangeColor.r, rangeColor.g, rangeColor.b, rangeColor.a * (0.9f + 0.1f * Mathf.Sin(t * 2.4f)));
    }
}
