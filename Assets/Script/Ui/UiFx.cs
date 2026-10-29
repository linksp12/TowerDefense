using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인·스토리 화면 연출용 공용 그림(빛 번짐·그라데이션·비네트·안개)과 UI 생성 도우미.
/// 그림은 실행 중 한 번 만들어 재사용한다(에셋 파일 없음).
/// </summary>
public static class UiFx
{
    private static Sprite softCircle, vignette, verticalFade, horizontalFade, fog, pixel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        softCircle = vignette = verticalFade = horizontalFade = fog = pixel = null;
    }

    // 가운데가 밝고 가장자리로 부드럽게 사라지는 원(빛 번짐, 섬광)
    public static Sprite SoftCircle => softCircle != null ? softCircle : softCircle = Make(128, 128, (x, y) =>
    {
        float d = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f)) * 2f;
        float a = Mathf.Clamp01(1f - d);
        return new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
    });

    // 가장자리가 어두운 화면 테두리
    public static Sprite Vignette => vignette != null ? vignette : vignette = Make(256, 144, (x, y) =>
    {
        float dx = (x - 0.5f) * 2f, dy = (y - 0.5f) * 2f;
        float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
        float a = Mathf.Clamp01((d - 0.55f) / 0.75f);
        return new Color(0f, 0f, 0f, a * a);
    });

    // 아래(불투명)에서 위(투명)로 사라지는 띠
    public static Sprite VerticalFade => verticalFade != null ? verticalFade : verticalFade = Make(4, 128, (x, y) =>
    {
        float a = Mathf.Clamp01(1f - y);
        return new Color(1f, 1f, 1f, a * a);
    });

    // 흐르는 안개(가로로 이어 붙여도 이음새가 없게 가로 방향은 주기적인 노이즈)
    public static Sprite Fog => fog != null ? fog : fog = Make(256, 64, (x, y) =>
    {
        float n = 0f, amp = 0.5f, freq = 2f;
        for (int o = 0; o < 4; o++)
        {
            float px = Mathf.Cos(x * Mathf.PI * 2f) * freq + 10f, py = Mathf.Sin(x * Mathf.PI * 2f) * freq + 10f;
            n += amp * Mathf.PerlinNoise(px + o * 3.1f, py + y * freq * 1.5f + o * 7.7f);
            amp *= 0.5f;
            freq *= 2f;
        }
        float band = Mathf.Sin(y * Mathf.PI);
        return new Color(1f, 1f, 1f, Mathf.Clamp01((n - 0.35f) * 1.6f) * band);
    }, TextureWrapMode.Repeat);

    // 왼쪽(불투명)에서 오른쪽(투명)으로 사라지는 띠
    public static Sprite HorizontalFade => horizontalFade != null ? horizontalFade : horizontalFade = Make(128, 4, (x, y) =>
    {
        float a = Mathf.Clamp01(1f - x);
        return new Color(1f, 1f, 1f, a * a);
    });

    public static Sprite Pixel => pixel != null ? pixel : pixel = Make(2, 2, (x, y) => Color.white);

    private static Sprite Make(int w, int h, System.Func<float, float, Color> shade, TextureWrapMode wrap = TextureWrapMode.Clamp)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = FilterMode.Bilinear };
        var colors = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                colors[y * w + x] = shade((x + 0.5f) / w, (y + 0.5f) / h);
        tex.SetPixels(colors);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static Image Image(string name, Transform parent, Sprite sprite, Color color)
    {
        var rect = Rect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    public static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
    public static float EaseInOut(float t) => t * t * (3f - 2f * t);
}

/// <summary>
/// UI 위를 떠다니는 입자(불씨·먼지·빛 알갱이). 정해진 영역에서 태어나 속도대로 움직이고 깜빡이며 사라진다.
/// 게임 시간과 무관하게(일시정지·결과창 영향 없이) 움직인다.
/// </summary>
public sealed class UiParticleField : MonoBehaviour
{
    private sealed class Particle
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Velocity;
        public float Age, Life, Size, Phase;
        public Color Color;
    }

    public Color[] palette = { new Color(1f, 0.6f, 0.2f) };
    public Vector2 sizeRange = new Vector2(3f, 6f);
    public Vector2 speedMin = new Vector2(-10f, 30f);
    public Vector2 speedMax = new Vector2(10f, 70f);
    public Vector2 lifeRange = new Vector2(3f, 6f);
    [Tooltip("초당 생성 수")] public float rate = 6f;
    [Tooltip("생성 영역(부모 기준 0~1)")] public Rect spawnArea = new Rect(0f, 0f, 1f, 0.2f);
    public float sway = 12f;
    public float twinkle = 0f;
    public bool softGlow;
    public int maxCount = 80;

    private readonly List<Particle> live = new List<Particle>();
    private readonly Stack<Particle> pool = new Stack<Particle>();
    private RectTransform area;
    private float carry;

    public static UiParticleField Create(string name, Transform parent)
    {
        var rect = UiFx.Rect(name, parent);
        UiFx.Stretch(rect);
        return rect.gameObject.AddComponent<UiParticleField>();
    }

    private void Awake() => area = (RectTransform)transform;

    // 처음 열었을 때 화면이 비어 보이지 않게 미리 채운다.
    public void Prewarm(float seconds)
    {
        for (float t = 0f; t < seconds; t += 0.1f) Step(0.1f);
    }

    private void Update() => Step(Mathf.Min(Time.unscaledDeltaTime, 0.1f));

    private void Step(float dt)
    {
        if (area == null) area = (RectTransform)transform;
        Rect bounds = area.rect;
        carry += rate * dt;
        while (carry >= 1f)
        {
            carry -= 1f;
            if (live.Count < maxCount) Spawn(bounds);
        }
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Particle p = live[i];
            p.Age += dt;
            if (p.Age >= p.Life)
            {
                p.Rect.gameObject.SetActive(false);
                pool.Push(p);
                live.RemoveAt(i);
                continue;
            }
            float t = p.Age / p.Life;
            Vector2 pos = p.Rect.anchoredPosition + p.Velocity * dt;
            pos.x += Mathf.Sin((p.Age + p.Phase) * 1.7f) * sway * dt;
            p.Rect.anchoredPosition = pos;
            float fade = Mathf.Min(1f, t * 6f) * (1f - t * t);
            if (twinkle > 0f) fade *= 1f - twinkle * 0.5f * (1f + Mathf.Sin((p.Age + p.Phase) * 9f));
            Color c = p.Color;
            c.a *= fade;
            p.Image.color = c;
        }
    }

    private void Spawn(Rect bounds)
    {
        Particle p = pool.Count > 0 ? pool.Pop() : CreateParticle();
        p.Rect.gameObject.SetActive(true);
        p.Size = Random.Range(sizeRange.x, sizeRange.y);
        p.Rect.sizeDelta = Vector2.one * (softGlow ? p.Size * 3f : p.Size);
        p.Rect.anchoredPosition = new Vector2(
            bounds.xMin + bounds.width * (spawnArea.xMin + Random.value * spawnArea.width),
            bounds.yMin + bounds.height * (spawnArea.yMin + Random.value * spawnArea.height));
        p.Velocity = new Vector2(Random.Range(speedMin.x, speedMax.x), Random.Range(speedMin.y, speedMax.y));
        p.Life = Random.Range(lifeRange.x, lifeRange.y);
        p.Age = 0f;
        p.Phase = Random.value * 10f;
        p.Color = palette[Random.Range(0, palette.Length)];
        p.Image.sprite = softGlow ? UiFx.SoftCircle : UiFx.Pixel;
        live.Add(p);
    }

    private Particle CreateParticle()
    {
        var image = UiFx.Image("Particle", transform, UiFx.Pixel, Color.clear);
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        return new Particle { Rect = rect, Image = image };
    }

    public void Clear()
    {
        foreach (var p in live)
        {
            p.Rect.gameObject.SetActive(false);
            pool.Push(p);
        }
        live.Clear();
    }
}
