using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화상 표시 공용 부품. 몸 가장자리 도트 불꽃(단계마다 1개, 최대 3개)과 위로 튀는 불씨를 그린다.
/// 저격·감전 화상(TowerBurnStack)과 마법 화염 지속 피해(MonsterStatus)가 같은 모습을 쓰도록,
/// 여러 출처가 단계를 요청하면 가장 높은 단계로 보여 준다.
/// </summary>
public sealed class BurnVisual : MonoBehaviour
{
    private sealed class Ember
    {
        public SpriteRenderer Renderer;
        public float Born;
        public Vector3 Start;
        public float Drift;
    }

    // 몸 크기 비율로 잡은 불꽃 자리: 오른쪽 어깨, 왼쪽 어깨, 머리 위
    private static readonly Vector2[] FlameAnchors = { new Vector2(0.32f, 0.35f), new Vector2(-0.34f, 0.25f), new Vector2(0.04f, 0.78f) };
    private const int MaxFlames = 3;
    private const float EmberLife = 0.5f;
    private const float EmberRise = 0.45f;

    private readonly Dictionary<Object, int> requests = new Dictionary<Object, int>();
    private readonly List<PixelLoop> flames = new List<PixelLoop>();
    private readonly List<Ember> embers = new List<Ember>();
    private int level;
    private float nextEmber;

    // source가 요청하는 불꽃 단계(0이면 요청 해제). 모든 요청이 없어지면 표시가 꺼진다.
    public static void Request(GameObject monster, Object source, int flameLevel)
    {
        if (monster == null) return;
        if (!monster.TryGetComponent(out BurnVisual visual))
        {
            if (flameLevel <= 0) return;
            visual = monster.AddComponent<BurnVisual>();
        }
        if (flameLevel <= 0) visual.requests.Remove(source);
        else visual.requests[source] = flameLevel;
        visual.Refresh();
    }

    private void Refresh()
    {
        level = 0;
        foreach (int value in requests.Values) level = Mathf.Max(level, value);
        level = Mathf.Min(MaxFlames, level);
        enabled = level > 0;
        if (enabled) PlaceFlames();
    }

    private void LateUpdate()
    {
        PlaceFlames();
        UpdateEmbers();
    }

    private void PlaceFlames()
    {
        CombatFeelSettings fx = CombatFeedback.Settings;
        if (fx == null) return;
        Bounds body = TowerBodyBounds.Of(gameObject);
        // 작은 몬스터는 작은 불꽃을 원래 도트 크기로, 큰 몬스터(보스 등)는 큰 불꽃을 몸에 맞게 키운다.
        bool small = body.size.y < 0.9f;
        CombatFeelSettings.Effect effect = small ? fx.burnFlameSmallFx : fx.burnFlameFx;
        float scale = small ? 1f : Mathf.Clamp(body.size.y * 0.45f / 0.625f, 1f, 2.5f);
        while (flames.Count < level)
        {
            PixelLoop flame = PixelLoop.Create("Burn Flame", transform, effect, 108);
            if (flame == null) return;
            flames.Add(flame);
        }
        for (int i = 0; i < flames.Count; i++)
        {
            bool show = i < level;
            flames[i].gameObject.SetActive(show);
            if (!show) continue;
            Vector2 anchor = FlameAnchors[i];
            // 불꽃 그림은 발밑 피벗이라 자리에서 위로 피어오른다.
            flames[i].transform.position = new Vector3(body.center.x + anchor.x * body.size.x,
                body.min.y + anchor.y * body.size.y, transform.position.z);
            flames[i].SetWorldScale(scale);
        }
    }

    private void UpdateEmbers()
    {
        CombatFeelSettings fx = CombatFeedback.Settings;
        if (fx == null || fx.emberFx.frames == null || fx.emberFx.frames.Length == 0) return;
        if (Time.time >= nextEmber)
        {
            nextEmber = Time.time + Random.Range(0.18f, 0.32f) / level;
            SpawnEmber(fx);
        }
        Sprite[] frames = fx.emberFx.frames;
        for (int i = embers.Count - 1; i >= 0; i--)
        {
            Ember ember = embers[i];
            float t = (Time.time - ember.Born) / EmberLife;
            if (t >= 1f || ember.Renderer == null)
            {
                if (ember.Renderer != null) Destroy(ember.Renderer.gameObject);
                embers.RemoveAt(i);
                continue;
            }
            ember.Renderer.sprite = frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length))];
            // 몸이 움직여도 몸 위에서 피어오르게 몸 기준으로 옮긴다.
            ember.Renderer.transform.localPosition = ember.Start +
                new Vector3(ember.Drift * t, EmberRise * t, 0f) / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        }
    }

    private void SpawnEmber(CombatFeelSettings fx)
    {
        Bounds body = TowerBodyBounds.Of(gameObject);
        var go = new GameObject("Burn Ember");
        go.transform.SetParent(transform, false);
        var renderer = TowerVfx.Renderer(go, fx.emberFx.frames[0], 109);
        Vector3 parentScale = transform.lossyScale;
        go.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
        Vector3 world = new Vector3(body.center.x + Random.Range(-0.4f, 0.4f) * body.size.x,
            body.min.y + Random.Range(0.4f, 0.9f) * body.size.y, transform.position.z);
        embers.Add(new Ember { Renderer = renderer, Born = Time.time, Start = transform.InverseTransformPoint(world),
            Drift = Random.Range(-0.15f, 0.15f) });
    }

    private void OnDisable()
    {
        foreach (var flame in flames)
            if (flame != null) Destroy(flame.gameObject);
        flames.Clear();
        foreach (var ember in embers)
            if (ember.Renderer != null) Destroy(ember.Renderer.gameObject);
        embers.Clear();
    }
}
