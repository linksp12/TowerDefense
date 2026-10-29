using UnityEngine;

/// <summary>새 타워 공격 선·탄 연출. 피해 계산과 별도로 모양·수명만 다룬다. 소리·타격 이펙트는 CombatFeedback이 맡는다.</summary>
internal static class TowerVfx
{
    private static TowerCombatVisuals settings;
    private static Material lineMaterial;
    public static TowerCombatVisuals Settings => settings != null ? settings :
        settings = Resources.Load<TowerCombatVisuals>("TowerCombatVisuals");

    public static Material LineMaterial
    {
        get
        {
            if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));
            return lineMaterial;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        settings = null;
        if (lineMaterial != null) Object.Destroy(lineMaterial);
        lineMaterial = null;
    }

    // 도트 그림을 원래 크기(PPU 그대로)로 그린다. 픽셀 크기가 다른 전투 그림과 같아진다.
    public static SpriteRenderer Renderer(GameObject go, Sprite sprite, int sortingOrder = 105)
    {
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Effects";
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    public static LineRenderer Line(GameObject go, Color color, float width, int sortingOrder = 104)
    {
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = LineMaterial;
        line.useWorldSpace = true;
        line.sortingLayerName = "Effects";
        line.sortingOrder = sortingOrder;
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.numCapVertices = 0;
        return line;
    }

    // 날아가는 탄 뒤에 남는 꼬리(저격 탄도선, 충전 레이저 잔광)
    public static TrailRenderer Trail(GameObject go, Color head, float width, float time)
    {
        var trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = LineMaterial;
        trail.sortingLayerName = "Effects";
        trail.sortingOrder = 103;
        trail.time = time;
        trail.minVertexDistance = 0.05f;
        trail.startWidth = width;
        trail.endWidth = 0f;
        trail.numCapVertices = 0;
        var tail = head;
        tail.a = 0f;
        trail.colorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(head, 0f), new GradientColorKey(head, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(head.a, 0f), new GradientAlphaKey(0f, 1f) }
        };
        return trail;
    }
}

/// <summary>
/// 지속 레이저 빔. 바깥 광채 + 안쪽 심지 두 줄로 그리고, 오래 맞힐수록 굵어지며
/// 붉은색 → 주황 → 흰빛으로 달아오른다. 끝에는 맞는 자리에 작은 에너지 구슬이 붙는다.
/// </summary>
internal sealed class TowerLaserBeam
{
    private static readonly Color GlowCold = new Color(0.85f, 0.06f, 0.12f, 0.45f);
    private static readonly Color GlowHot = new Color(1f, 0.45f, 0.12f, 0.6f);
    private static readonly Color CoreCold = new Color(1f, 0.38f, 0.32f, 1f);
    private static readonly Color CoreHot = new Color(1f, 0.97f, 0.86f, 1f);

    private readonly GameObject root;
    private readonly LineRenderer glow, core;
    private readonly PixelLoop hitGlow;
    private readonly float seed = Random.value * 100f;

    public TowerLaserBeam(Transform parent)
    {
        root = new GameObject("Sustained Laser");
        root.transform.SetParent(parent, false);
        glow = TowerVfx.Line(root, GlowCold, 0.15f, 103);
        glow.positionCount = 2;
        var coreObject = new GameObject("Core");
        coreObject.transform.SetParent(root.transform, false);
        core = TowerVfx.Line(coreObject, CoreCold, 0.05f, 104);
        core.positionCount = 2;
        CombatFeelSettings fx = CombatFeedback.Settings;
        hitGlow = fx != null ? PixelLoop.Create("Laser Hit Glow", root.transform, fx.energyChargeFx, 106) : null;
    }

    public void Show(Vector3 from, Vector3 to, float strength)
    {
        root.SetActive(true);
        // 빔이 살아 있는 느낌이 나게 굵기를 살짝 일렁인다.
        float flicker = 0.88f + 0.24f * Mathf.PerlinNoise(seed, Time.time * 18f);
        float coreWidth = Mathf.Lerp(0.05f, 0.12f, strength) * flicker;
        float glowWidth = Mathf.Lerp(0.15f, 0.32f, strength) * flicker;
        core.startWidth = coreWidth;
        core.endWidth = coreWidth * 0.8f;
        glow.startWidth = glowWidth;
        glow.endWidth = glowWidth * 0.85f;
        core.startColor = core.endColor = Color.Lerp(CoreCold, CoreHot, strength);
        glow.startColor = glow.endColor = Color.Lerp(GlowCold, GlowHot, strength);
        core.SetPosition(0, from);
        core.SetPosition(1, to);
        glow.SetPosition(0, from);
        glow.SetPosition(1, to);
        if (hitGlow != null)
        {
            hitGlow.transform.position = to;
            hitGlow.SetWorldScale(Mathf.Lerp(0.55f, 1f, strength) * flicker);
        }
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public void Destroy()
    {
        if (root != null) Object.Destroy(root);
    }
}

/// <summary>
/// 감전 번개. 짧은 시간 동안 지그재그 모양을 계속 바꿔 지직거리게 하고, 가지 번개 하나를 함께 그린다.
/// 바깥 광채 + 진한 파란 테두리 + 흰 심지 세 겹이라 눈 맵처럼 밝은 바닥에서도 보인다.
/// </summary>
internal sealed class TowerElectricArc : MonoBehaviour
{
    private static readonly Color GlowColor = new Color(0.25f, 0.7f, 1f, 0.4f);
    private static readonly Color EdgeColor = new Color(0.08f, 0.28f, 0.75f, 0.95f);
    private static readonly Color CoreColor = new Color(0.88f, 0.99f, 1f, 1f);
    private const float Lifetime = 0.22f;
    private const float Rejag = 0.035f;

    private LineRenderer glow, edge, core, branch;
    private Vector3 from, to;
    private float born, nextJag;

    public static void Spawn(Vector3 from, Vector3 to)
    {
        var go = new GameObject("Electric Arc");
        var arc = go.AddComponent<TowerElectricArc>();
        arc.from = from;
        arc.to = to;
        arc.born = Time.time;
        arc.glow = TowerVfx.Line(go, GlowColor, 0.22f, 102);
        arc.edge = TowerVfx.Line(Child(go, "Edge"), EdgeColor, 0.09f, 103);
        arc.core = TowerVfx.Line(Child(go, "Core"), CoreColor, 0.04f, 104);
        arc.branch = TowerVfx.Line(Child(go, "Branch"), EdgeColor, 0.045f, 103);
        arc.Rebuild();
    }

    private static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private void Update()
    {
        float age = Time.time - born;
        if (age >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }
        if (Time.time >= nextJag) Rebuild();
        float alpha = 1f - Mathf.Clamp01((age - Lifetime * 0.5f) / (Lifetime * 0.5f));
        SetAlpha(glow, GlowColor, alpha);
        SetAlpha(edge, EdgeColor, alpha);
        SetAlpha(core, CoreColor, alpha);
        SetAlpha(branch, EdgeColor, alpha * 0.8f);
    }

    private static void SetAlpha(LineRenderer line, Color color, float alpha)
    {
        color.a *= alpha;
        line.startColor = line.endColor = color;
    }

    private void Rebuild()
    {
        nextJag = Time.time + Rejag;
        Vector3 delta = to - from;
        int count = Mathf.Clamp(Mathf.CeilToInt(delta.magnitude / 0.28f), 4, 18);
        Vector3 side = Vector3.Cross(delta.normalized, Vector3.forward);
        glow.positionCount = edge.positionCount = core.positionCount = count;
        Vector3 mid = from;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            float offset = i == 0 || i == count - 1 ? 0f : Random.Range(-0.16f, 0.16f);
            Vector3 point = Vector3.Lerp(from, to, t) + side * offset;
            glow.SetPosition(i, point);
            edge.SetPosition(i, point);
            core.SetPosition(i, point);
            if (i == count / 2) mid = point;
        }
        // 가운데쯤에서 옆으로 갈라지는 짧은 가지
        Vector3 branchEnd = mid + (Quaternion.Euler(0, 0, Random.Range(-60f, 60f)) * delta.normalized) * 0.45f
            + side * Random.Range(-0.25f, 0.25f);
        branch.positionCount = 3;
        branch.SetPosition(0, mid);
        branch.SetPosition(1, Vector3.Lerp(mid, branchEnd, 0.5f) + side * Random.Range(-0.08f, 0.08f));
        branch.SetPosition(2, branchEnd);
    }
}

/// <summary>몬스터 몸 크기. 그림 사각형은 투명 여백이 커서 충돌체를 우선 쓴다.</summary>
internal static class TowerBodyBounds
{
    public static Bounds Of(GameObject monster)
    {
        if (monster.TryGetComponent(out Collider2D collider) && collider.enabled) return collider.bounds;
        if (monster.TryGetComponent(out SpriteRenderer renderer)) return renderer.bounds;
        return new Bounds(monster.transform.position, Vector3.one * 0.5f);
    }
}
