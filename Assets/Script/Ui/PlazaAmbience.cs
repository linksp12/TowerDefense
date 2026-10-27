using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 광장 분위기 연출: 불빛 번짐(가로등·화로·모닥불·횃불), 굴뚝 연기, 불티, 분수 물방울,
/// 천천히 지나가는 구름 그림자, 가끔 날아가는 새, 꽃 주변 나비. 모두 월드 좌표의 작은 그림으로 그린다.
/// 클릭을 막지 않고(충돌체 없음), 광장 NPC·건물 그림은 건드리지 않는다.
/// </summary>
public sealed class PlazaAmbience : MonoBehaviour
{
    [Serializable]
    public struct Glow
    {
        public Vector2 position;
        public Color color;
        [Tooltip("빛 번짐 지름(월드 단위)")] public float size;
        [Range(0f, 1f)] public float flicker;
    }

    [Serializable]
    public struct Emitter
    {
        public Vector2 position;
        [Tooltip("초당 생성 수")] public float rate;
    }

    [Header("불빛 번짐")]
    [SerializeField] private Glow[] glows;
    [Header("연기·불티·물방울")]
    [SerializeField] private Emitter[] smoke;
    [SerializeField] private Emitter[] sparks;
    [SerializeField] private Emitter[] fountainSpouts;
    [Header("하늘")]
    [SerializeField] private int cloudShadows = 3;
    [SerializeField] private Vector2 birdInterval = new Vector2(10f, 20f);
    [Header("나비 (꽃·덤불 주변)")]
    [SerializeField] private Vector2[] butterflies;
    [Tooltip("화면 범위(월드). 구름·새가 이 밖에서 들어와 지나간다.")]
    [SerializeField] private Rect area = new Rect(-9f, -5f, 18f, 10f);

    private sealed class Puff
    {
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public float Age, Life, StartSize, EndSize, Alpha, Gravity;
        public Color Color;
    }

    private sealed class Flyer
    {
        public Transform Root;
        public SpriteRenderer Body, Shadow;
        public Vector2 Velocity;
        public float Phase;
        public Sprite[] Frames;
    }

    private const int GlowOrder = 3, PuffOrder = 4, CloudOrder = 40, BirdOrder = 60;
    private readonly List<Puff> live = new List<Puff>();
    private readonly Stack<Puff> pool = new Stack<Puff>();
    private readonly List<SpriteRenderer> glowRenderers = new List<SpriteRenderer>();
    private readonly List<Transform> clouds = new List<Transform>();
    private readonly List<Flyer> birds = new List<Flyer>();
    private readonly List<Flyer> butterflyFlyers = new List<Flyer>();
    private float[] carry;
    private float nextBirds;
    private Sprite softCircle, pixel;
    private Sprite[] birdFrames, butterflyFrames;

    private void Start()
    {
        softCircle = UiFx.SoftCircle;
        pixel = UiFx.Pixel;
        birdFrames = PixelSprites(BirdArt, 4, new Color32(40, 36, 44, 255));
        butterflyFrames = PixelSprites(ButterflyArt, 3, new Color32(250, 240, 200, 255), new Color32(255, 170, 70, 255));

        foreach (var glow in glows)
        {
            var renderer = MakeRenderer("Glow", softCircle, GlowOrder);
            renderer.transform.position = glow.position;
            renderer.transform.localScale = Vector3.one * glow.size / softCircle.bounds.size.x;
            renderer.color = glow.color;
            glowRenderers.Add(renderer);
        }
        carry = new float[smoke.Length + sparks.Length + fountainSpouts.Length];

        for (int i = 0; i < cloudShadows; i++)
        {
            var cloud = MakeRenderer("CloudShadow", softCircle, CloudOrder);
            cloud.color = new Color(0.05f, 0.06f, 0.1f, 0.075f);
            float w = UnityEngine.Random.Range(5f, 8f), h = w * UnityEngine.Random.Range(0.45f, 0.6f);
            cloud.transform.localScale = new Vector3(w, h, 1f) / softCircle.bounds.size.x;
            cloud.transform.position = new Vector2(area.xMin + area.width * (i + UnityEngine.Random.value) / cloudShadows,
                UnityEngine.Random.Range(area.yMin, area.yMax));
            clouds.Add(cloud.transform);
        }

        foreach (var anchor in butterflies)
        {
            var flyer = MakeFlyer("Butterfly", butterflyFrames, false);
            flyer.Root.position = anchor;
            flyer.Phase = UnityEngine.Random.value * 10f;
            flyer.Velocity = anchor; // 나비는 기준점을 Velocity 칸에 보관한다.
            butterflyFlyers.Add(flyer);
        }
        nextBirds = Time.time + UnityEngine.Random.Range(2f, 5f);
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        float now = Time.time;

        // 불빛: 천천히 숨쉬듯 + 작은 떨림
        for (int i = 0; i < glowRenderers.Count; i++)
        {
            Glow glow = glows[i];
            float wave = Mathf.Sin(now * 2.1f + i * 1.7f) * 0.5f + (Mathf.PerlinNoise(now * 6f, i * 3.1f) - 0.5f);
            Color c = glow.color;
            c.a *= 1f + wave * glow.flicker;
            glowRenderers[i].color = c;
            glowRenderers[i].transform.localScale = Vector3.one * glow.size * (1f + wave * glow.flicker * 0.15f) / softCircle.bounds.size.x;
        }

        int k = 0;
        foreach (var e in smoke) Emit(ref carry[k++], e, dt, SpawnSmoke);
        foreach (var e in sparks) Emit(ref carry[k++], e, dt, SpawnSpark);
        foreach (var e in fountainSpouts) Emit(ref carry[k++], e, dt, SpawnDroplet);
        UpdatePuffs(dt);

        foreach (var cloud in clouds)
        {
            cloud.position += new Vector3(0.22f, 0.04f, 0f) * dt;
            if (cloud.position.x - cloud.localScale.x * softCircle.bounds.size.x * 0.5f > area.xMax)
                cloud.position = new Vector3(area.xMin - cloud.localScale.x * softCircle.bounds.size.x * 0.5f,
                    UnityEngine.Random.Range(area.yMin, area.yMax), 0f);
        }

        if (now >= nextBirds)
        {
            SpawnBirds();
            nextBirds = now + UnityEngine.Random.Range(birdInterval.x, birdInterval.y);
        }
        UpdateBirds(dt, now);
        UpdateButterflies(now);
    }

    private static void Emit(ref float carry, Emitter e, float dt, Action<Vector2> spawn)
    {
        carry += e.rate * dt;
        while (carry >= 1f)
        {
            carry -= 1f;
            spawn(e.position);
        }
    }

    // ──────── 연기·불티·물방울 ────────

    private void SpawnSmoke(Vector2 at)
    {
        Puff p = TakePuff(softCircle);
        p.Renderer.transform.position = at + new Vector2(UnityEngine.Random.Range(-0.05f, 0.05f), 0f);
        p.Velocity = new Vector2(UnityEngine.Random.Range(0.05f, 0.18f), UnityEngine.Random.Range(0.35f, 0.5f));
        p.Life = UnityEngine.Random.Range(2.6f, 3.6f);
        p.StartSize = 0.18f;
        p.EndSize = UnityEngine.Random.Range(0.75f, 1f);
        p.Alpha = 0.32f;
        p.Gravity = 0f;
        p.Color = new Color(0.78f, 0.76f, 0.74f);
    }

    private void SpawnSpark(Vector2 at)
    {
        Puff p = TakePuff(pixel);
        p.Renderer.transform.position = at + new Vector2(UnityEngine.Random.Range(-0.12f, 0.12f), UnityEngine.Random.Range(-0.05f, 0.05f));
        p.Velocity = new Vector2(UnityEngine.Random.Range(-0.15f, 0.15f), UnityEngine.Random.Range(0.5f, 1.1f));
        p.Life = UnityEngine.Random.Range(0.5f, 1f);
        p.StartSize = p.EndSize = UnityEngine.Random.Range(0.035f, 0.05f);
        p.Alpha = 1f;
        p.Gravity = -0.2f;
        p.Color = UnityEngine.Random.value < 0.5f ? new Color(1f, 0.75f, 0.3f) : new Color(1f, 0.45f, 0.15f);
    }

    private void SpawnDroplet(Vector2 at)
    {
        Puff p = TakePuff(pixel);
        p.Renderer.transform.position = at;
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        p.Velocity = new Vector2(Mathf.Cos(angle) * UnityEngine.Random.Range(0.15f, 0.45f), UnityEngine.Random.Range(0.6f, 0.95f));
        p.Life = UnityEngine.Random.Range(0.55f, 0.8f);
        p.StartSize = p.EndSize = UnityEngine.Random.Range(0.035f, 0.05f);
        p.Alpha = 0.95f;
        p.Gravity = 2.6f;
        p.Color = UnityEngine.Random.value < 0.6f ? new Color(0.75f, 0.92f, 1f) : Color.white;
    }

    private Puff TakePuff(Sprite sprite)
    {
        Puff p = pool.Count > 0 ? pool.Pop() : new Puff { Renderer = MakeRenderer("Puff", sprite, PuffOrder) };
        p.Renderer.sprite = sprite;
        p.Renderer.gameObject.SetActive(true);
        p.Age = 0f;
        live.Add(p);
        return p;
    }

    private void UpdatePuffs(float dt)
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Puff p = live[i];
            p.Age += dt;
            if (p.Age >= p.Life)
            {
                p.Renderer.gameObject.SetActive(false);
                pool.Push(p);
                live.RemoveAt(i);
                continue;
            }
            float t = p.Age / p.Life;
            p.Velocity.y -= p.Gravity * dt;
            p.Renderer.transform.position += (Vector3)(p.Velocity * dt);
            float size = Mathf.Lerp(p.StartSize, p.EndSize, Mathf.Sqrt(t));
            p.Renderer.transform.localScale = Vector3.one * size / p.Renderer.sprite.bounds.size.x;
            Color c = p.Color;
            c.a = p.Alpha * Mathf.Min(1f, t * 5f) * (1f - t);
            p.Renderer.color = c;
        }
    }

    // ──────── 새·나비 ────────

    private void SpawnBirds()
    {
        int count = UnityEngine.Random.Range(2, 5);
        bool toRight = UnityEngine.Random.value < 0.5f;
        float y = UnityEngine.Random.Range(area.yMin + area.height * 0.35f, area.yMax - 0.5f);
        for (int i = 0; i < count; i++)
        {
            var bird = MakeFlyer("Bird", birdFrames, true);
            float x = toRight ? area.xMin - 0.6f - i * 0.55f : area.xMax + 0.6f + i * 0.55f;
            bird.Root.position = new Vector2(x, y + (i % 2 == 0 ? i : -i) * 0.18f);
            bird.Velocity = new Vector2(toRight ? 2.3f : -2.3f, UnityEngine.Random.Range(-0.15f, 0.25f));
            bird.Body.flipX = !toRight;
            bird.Phase = UnityEngine.Random.value;
            birds.Add(bird);
        }
    }

    private void UpdateBirds(float dt, float now)
    {
        for (int i = birds.Count - 1; i >= 0; i--)
        {
            Flyer bird = birds[i];
            bird.Root.position += (Vector3)(bird.Velocity * dt);
            bird.Body.sprite = bird.Frames[Mathf.FloorToInt((now + bird.Phase) * 7f) % bird.Frames.Length];
            if (bird.Shadow != null)
            {
                bird.Shadow.sprite = bird.Body.sprite;
                bird.Shadow.flipX = bird.Body.flipX;
            }
            float x = bird.Root.position.x;
            if (x > area.xMax + 3f || x < area.xMin - 3f)
            {
                Destroy(bird.Root.gameObject);
                birds.RemoveAt(i);
            }
        }
    }

    private void UpdateButterflies(float now)
    {
        foreach (var b in butterflyFlyers)
        {
            float t = now * 0.7f + b.Phase;
            Vector2 anchor = b.Velocity;
            Vector2 offset = new Vector2(Mathf.Sin(t) * 0.45f + Mathf.Sin(t * 2.3f) * 0.15f, Mathf.Sin(t * 1.6f) * 0.22f + 0.25f);
            float dx = Mathf.Cos(t) * 0.45f + Mathf.Cos(t * 2.3f) * 0.35f;
            b.Root.position = anchor + offset;
            b.Body.flipX = dx < 0f;
            b.Body.sprite = b.Frames[Mathf.FloorToInt((now + b.Phase) * 9f) % b.Frames.Length];
        }
    }

    private Flyer MakeFlyer(string name, Sprite[] frames, bool shadow)
    {
        var root = new GameObject(name).transform;
        root.SetParent(transform, false);
        var body = MakeRenderer("Body", frames[0], BirdOrder, root);
        SpriteRenderer shade = null;
        if (shadow)
        {
            // 날아가는 새 아래 땅에 비치는 그림자(아래로 떨어뜨려 높이감)
            shade = MakeRenderer("Shadow", frames[0], CloudOrder + 1, root);
            shade.color = new Color(0f, 0f, 0f, 0.18f);
            shade.transform.localPosition = new Vector3(0.6f, -1.4f, 0f);
            shade.transform.localScale = new Vector3(0.9f, 0.6f, 1f);
        }
        return new Flyer { Root = root, Body = body, Shadow = shade, Frames = frames };
    }

    private SpriteRenderer MakeRenderer(string name, Sprite sprite, int order, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    // ──────── 도트 그림(새·나비)을 코드로 만든다 ────────

    private static readonly string[][] BirdArt =
    {
        new[] { "#.......#", ".#.....#.", "..#.#.#..", "...###...", "....#...." },
        new[] { ".........", ".........", "..#####..", ".#.###.#.", "#...#...#" },
    };

    private static readonly string[][] ButterflyArt =
    {
        new[] { "oo.oo", "oo#oo", ".o#o.", "o.#.o" },
        new[] { ".....", ".o#o.", "oo#oo", "....." },
    };

    private static Sprite[] PixelSprites(string[][] art, int block, Color32 main, Color32? accent = null)
    {
        var sprites = new Sprite[art.Length];
        for (int f = 0; f < art.Length; f++)
        {
            string[] rows = art[f];
            int w = rows[0].Length * block, h = rows.Length * block;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[w * h];
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char ch = rows[y][x];
                    if (ch == '.') continue;
                    Color32 c = ch == 'o' && accent.HasValue ? accent.Value : ch == '#' && accent.HasValue ? new Color32(60, 40, 30, 255) : main;
                    for (int by = 0; by < block; by++)
                        for (int bx = 0; bx < block; bx++)
                            pixels[((rows.Length - 1 - y) * block + by) * w + x * block + bx] = c;
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            sprites[f] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        return sprites;
    }
}
