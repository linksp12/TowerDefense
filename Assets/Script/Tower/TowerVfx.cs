using UnityEngine;

/// <summary>공격 선/스프라이트 연출. 피해 계산과 별도로 크기·수명만 다룬다.</summary>
internal static class TowerVfx
{
    private static TowerCombatVisuals settings;
    private static Material lineMaterial;
    public static TowerCombatVisuals Settings => settings != null ? settings :
        settings = Resources.Load<TowerCombatVisuals>("TowerCombatVisuals");
    public static readonly Color LaserColor = new Color(1, 0.12f, 0.07f);
    public static readonly Color ElectricColor = new Color(0.2f, 0.85f, 1);

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

    public static SpriteRenderer Renderer(GameObject go, Sprite sprite, float width)
    {
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Effects";
        renderer.sortingOrder = 105;
        if (sprite != null) go.transform.localScale = Vector3.one * width / Mathf.Max(0.01f, sprite.bounds.size.x);
        return renderer;
    }

    public static LineRenderer Line(GameObject go, Color color, float width)
    {
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = LineMaterial;
        line.useWorldSpace = true;
        line.sortingLayerName = "Effects";
        line.sortingOrder = 104;
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.numCapVertices = 0;
        return line;
    }

    public static void Burst(Sprite sprite, Vector3 position, float width, float duration = 0.22f)
    {
        if (sprite == null) return;
        var go = new GameObject("Tower Impact");
        go.transform.position = position;
        var renderer = Renderer(go, sprite, width);
        go.AddComponent<TowerSpritePulse>().Initialize(renderer, duration);
    }

    public static void Flash(Vector3 from, Vector3 to, Color color, bool jagged, float width = 0.06f)
    {
        var go = new GameObject(jagged ? "Electric Discharge" : "Energy Flash");
        var line = Line(go, color, width);
        int count = jagged ? 9 : 2;
        line.positionCount = count;
        Vector3 perpendicular = Vector3.Cross((to - from).normalized, Vector3.forward);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            float offset = i == 0 || i == count - 1 ? 0 : (i % 2 == 0 ? -0.07f : 0.07f);
            line.SetPosition(i, Vector3.Lerp(from, to, t) + perpendicular * offset);
        }
        Object.Destroy(go, jagged ? 0.12f : 0.08f);
    }

    public static void Play(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;
        float volume = Settings != null ? Settings.soundVolume : 0.3f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume);
            return;
        }
        var go = new GameObject("Tower SFX");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        AudioManager.PlaySFXOn(source, clip, volume);
        Object.Destroy(go, clip.length + 0.1f);
    }
}

internal sealed class TowerSpritePulse : MonoBehaviour
{
    private SpriteRenderer visual;
    private Vector3 fullScale;
    private float start, duration;

    public void Initialize(SpriteRenderer renderer, float lifetime)
    {
        visual = renderer;
        duration = Mathf.Max(0.01f, lifetime);
        start = Time.time;
        fullScale = transform.localScale;
    }

    private void Update()
    {
        float t = (Time.time - start) / duration;
        if (t >= 1) { Destroy(gameObject); return; }
        transform.localScale = fullScale * Mathf.Lerp(0.5f, 1.15f, Mathf.Clamp01(t * 4));
        if (visual != null) visual.color = new Color(1, 1, 1, 1 - t * t);
    }
}
