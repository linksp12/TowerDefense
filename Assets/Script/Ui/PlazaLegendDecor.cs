using UnityEngine;

/// <summary>
/// 하드코어 완주 후 광장 장식: 남쪽 길목의 황금 날개 석상(은은한 빛)과 축제 깃발 기둥 4개.
/// 그림은 광장 원본 시트(RPG.pixcel)의 석상·깃발 기둥을 다시 칠한 것이라 화풍이 같다.
/// PlazaAmbience가 해금됐을 때만 붙인다. 충돌체가 없어 NPC 클릭을 막지 않는다.
/// </summary>
public sealed class PlazaLegendDecor : MonoBehaviour
{
    // 광장맵 기준 월드 좌표(카메라 크기 5, 광장 그림 중앙 0,0). 그림 기준점은 아래 가운데.
    private static readonly Vector2 StatueBase = new Vector2(0f, -3.95f);
    private static readonly Vector2[] BannerBases =
    {
        new Vector2(-1.75f, 2.85f), new Vector2(1.75f, 2.85f),   // 북쪽 아치 양옆
        new Vector2(-1.6f, -4.0f), new Vector2(1.6f, -4.0f),     // 석상 양옆
    };
    private const int DecorOrder = 2;

    private SpriteRenderer glow;

    private void Start()
    {
        var statue = Resources.Load<Sprite>("Plaza/legend_statue");
        if (statue != null)
        {
            glow = MakeRenderer("LegendGlow", UiFx.SoftCircle, DecorOrder - 1);
            glow.transform.position = StatueBase + new Vector2(0f, 0.85f);
            glow.transform.localScale = new Vector3(2.4f, 2.4f, 1f) / Mathf.Max(0.01f, UiFx.SoftCircle.bounds.size.x);
            MakeRenderer("LegendStatue", statue, DecorOrder).transform.position = StatueBase;
        }

        Sprite red = Resources.Load<Sprite>("Plaza/festival_banner_red");
        Sprite blue = Resources.Load<Sprite>("Plaza/festival_banner_blue");
        for (int i = 0; i < BannerBases.Length; i++)
        {
            Sprite sprite = i % 2 == 0 ? red : blue;
            if (sprite == null) continue;
            MakeRenderer("FestivalBanner", sprite, DecorOrder).transform.position = BannerBases[i];
        }
    }

    private SpriteRenderer MakeRenderer(string name, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void Update()
    {
        if (glow != null)
            glow.color = new Color(1f, 0.82f, 0.4f, 0.22f + 0.08f * Mathf.Sin(Time.unscaledTime * 1.7f));
    }
}
