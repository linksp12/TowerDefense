using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방어기지(방패 문장) 연출: 뒤에서 도는 보호막 고리, 위쪽 체력바(깎인 만큼 흰 잔상), 피격 시 튕김·붉은 번쩍임,
/// 위험 상태의 빠른 맥박과 연기. GameManager가 체력이 바뀔 때 알려 주고, 'Shield' 태그 오브젝트마다 자동으로 붙는다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class DefenseBase : MonoBehaviour
{
    private static readonly List<DefenseBase> Bases = new List<DefenseBase>();

    private const float HitDuration = 0.3f;
    private const float RingRadiusRatio = 21.5f / 24f; // 오라 그림 반지름 대비 고리 위치

    private SpriteRenderer shield;
    private Transform fxRoot;
    private SpriteRenderer aura;
    private SpriteRenderer barFill;
    private SpriteRenderer barTrail;
    private Vector3 restScale;
    private Color restColor;
    private float barInnerWidth;
    private float barInnerHeight;
    private float targetRatio = 1f;
    private float shownRatio = 1f;
    private float trailRatio = 1f;
    private int state;
    private float hitTime = -1f;
    private float smokeTimer;

    private static readonly Color HitColor = new Color(1f, 0.42f, 0.38f, 1f);
    private static readonly Color BarHealthy = new Color32(111, 211, 106, 255);
    private static readonly Color BarDamaged = new Color32(242, 200, 75, 255);
    private static readonly Color BarCritical = new Color32(229, 83, 61, 255);

    // 현재 씬의 모든 기지('Shield' 태그)에 연출을 붙인다.
    public static void AttachAll()
    {
        foreach (GameObject candidate in GameObject.FindGameObjectsWithTag("Shield"))
        {
            if (candidate.GetComponent<SpriteRenderer>() != null && candidate.GetComponent<DefenseBase>() == null)
                candidate.AddComponent<DefenseBase>();
        }
    }

    // 체력이 바뀔 때: 모든 기지의 그림·체력바·오라 색을 같은 상태로 맞춘다.
    public static void UpdateAll(float hpRatio, int shieldState, Sprite shieldSprite)
    {
        foreach (DefenseBase defenseBase in Bases)
            defenseBase.Apply(hpRatio, shieldState, shieldSprite);
    }

    // 몬스터가 도착한 위치에서 가장 가까운 기지가 맞는다. 위치가 없으면 첫 기지.
    public static void PlayHit(Vector3? hitPosition, bool cracked)
    {
        DefenseBase target = null;
        float best = float.MaxValue;
        foreach (DefenseBase defenseBase in Bases)
        {
            float distance = hitPosition.HasValue
                ? Vector2.Distance(hitPosition.Value, defenseBase.transform.position)
                : 0f;
            if (target == null || distance < best)
            {
                target = defenseBase;
                best = distance;
            }
        }

        if (target != null)
            target.Hit(cracked);
        else
            CombatFeedback.BaseHit(hitPosition ?? Vector3.zero, cracked);
    }

    private void Awake()
    {
        shield = GetComponent<SpriteRenderer>();
        restScale = transform.localScale;
        restColor = shield.color;
        Build();
    }

    private void OnEnable() => Bases.Add(this);

    private void OnDisable()
    {
        Bases.Remove(this);
        transform.localScale = restScale;
        if (shield != null)
            shield.color = restColor;
    }

    private void Build()
    {
        Bounds bounds = shield.bounds;
        // 기지가 튕길 때 함께 커지지 않도록 형제 오브젝트로 두고 월드 크기로 맞춘다.
        fxRoot = new GameObject(name + "_BaseFx").transform;
        fxRoot.SetParent(transform.parent, false);
        fxRoot.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);

        CombatFeelSettings settings = CombatFeedback.Settings;
        if (settings != null && settings.baseAura != null)
        {
            aura = NewRenderer("Aura", settings.baseAura, shield.sortingLayerID, shield.sortingOrder - 1);
            float ringDiameter = settings.baseAura.bounds.size.x * RingRadiusRatio;
            float scale = bounds.size.x * 1.25f / Mathf.Max(0.01f, ringDiameter);
            aura.transform.localScale = Vector3.one * scale;
            aura.color = settings.auraHealthy;
        }

        // 체력바: 기지 위쪽, 몬스터 체력바와 같은 픽셀 테두리
        if (BattleUiSkin.HpFrame != null && BattleUiSkin.HpFill != null)
        {
            int effects = SortingLayer.NameToID("Effects");
            float width = bounds.size.x * 0.9f;
            const float height = 0.28f;
            const float border = 0.08f;
            barInnerWidth = width - border * 2f;
            barInnerHeight = height - border * 2f;
            Vector3 barPosition = new Vector3(0f, bounds.extents.y + 0.28f, 0f);

            SpriteRenderer frame = NewRenderer("HpBarFrame", BattleUiSkin.HpFrame, effects, 40);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = new Vector2(width, height);
            frame.transform.localPosition = barPosition;

            barTrail = NewRenderer("HpBarTrail", BattleUiSkin.HpFill, effects, 41);
            barTrail.drawMode = SpriteDrawMode.Sliced;
            barTrail.color = new Color(1f, 0.95f, 0.85f, 0.9f);
            barTrail.transform.localPosition = barPosition;

            barFill = NewRenderer("HpBarFill", BattleUiSkin.HpFill, effects, 42);
            barFill.drawMode = SpriteDrawMode.Sliced;
            barFill.transform.localPosition = barPosition;
            SetBar(barTrail, 1f);
            SetBar(barFill, 1f);
            barFill.color = BarHealthy;
        }
    }

    private SpriteRenderer NewRenderer(string objectName, Sprite sprite, int sortingLayer, int order)
    {
        var go = new GameObject(objectName, typeof(SpriteRenderer));
        go.transform.SetParent(fxRoot, false);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void SetBar(SpriteRenderer bar, float ratio)
    {
        if (bar == null)
            return;
        float width = Mathf.Max(0f, barInnerWidth * Mathf.Clamp01(ratio));
        bar.enabled = width > 0.01f;
        bar.size = new Vector2(Mathf.Max(width, 0.02f), barInnerHeight);
        Vector3 position = bar.transform.localPosition;
        position.x = -barInnerWidth * 0.5f + width * 0.5f;
        bar.transform.localPosition = position;
    }

    private void Apply(float hpRatio, int shieldState, Sprite shieldSprite)
    {
        targetRatio = Mathf.Clamp01(hpRatio);
        state = shieldState;
        if (shieldSprite != null)
            shield.sprite = shieldSprite;
        if (barFill != null)
            barFill.color = state == 0 ? BarHealthy : state == 1 ? BarDamaged : BarCritical;
    }

    private void Hit(bool cracked)
    {
        hitTime = 0f;
        CombatFeedback.BaseHit(fxRoot != null ? fxRoot.position : transform.position, cracked);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float time = Time.time;
        CombatFeelSettings settings = CombatFeedback.Settings;

        // 보호막 고리: 상태가 나쁠수록 빨리 돌고 빠르게 맥박친다.
        if (aura != null && settings != null)
        {
            float spin = state == 0 ? 12f : state == 1 ? 22f : 48f;
            float pulseSpeed = state == 2 ? 9f : 2.4f;
            aura.transform.Rotate(0f, 0f, -spin * dt);
            Color color = state == 0 ? settings.auraHealthy : state == 1 ? settings.auraDamaged : settings.auraCritical;
            color.a *= 0.7f + 0.3f * Mathf.Sin(time * pulseSpeed);
            aura.color = color;
        }

        // 체력바: 깎인 칸은 바로 줄고, 흰 잔상이 뒤따라 줄어든다.
        if (barFill != null)
        {
            shownRatio = targetRatio;
            trailRatio = trailRatio > shownRatio ? Mathf.MoveTowards(trailRatio, shownRatio, dt * 0.6f) : shownRatio;
            SetBar(barFill, shownRatio);
            SetBar(barTrail, trailRatio);
            // 위험 상태에서는 빨강과 밝은 빨강 사이로 깜빡여 눈에 띄게 한다.
            barFill.color = state == 2
                ? Color.Lerp(BarCritical, new Color(1f, 0.75f, 0.6f, 1f), 0.5f + 0.5f * Mathf.Sin(time * 10f))
                : state == 1 ? BarDamaged : BarHealthy;
        }

        // 피격: 짧게 찌그러졌다 튕기며 붉게 번쩍인다.
        if (hitTime >= 0f)
        {
            hitTime += dt;
            float p = Mathf.Clamp01(hitTime / HitDuration);
            float bounce = Mathf.Sin(p * Mathf.PI * 2f) * (1f - p) * 0.14f;
            transform.localScale = new Vector3(restScale.x * (1f + bounce), restScale.y * (1f - bounce), restScale.z);
            shield.color = Color.Lerp(HitColor, restColor, p * p);
            if (p >= 1f)
            {
                hitTime = -1f;
                transform.localScale = restScale;
                shield.color = restColor;
            }
        }

        // 위험 상태: 기지 위로 연기가 피어오른다.
        if (state == 2)
        {
            smokeTimer -= dt;
            if (smokeTimer <= 0f)
            {
                smokeTimer = 0.7f;
                Bounds bounds = shield.bounds;
                CombatFeedback.BaseSmoke(new Vector3(
                    bounds.center.x + Random.Range(-bounds.extents.x, bounds.extents.x) * 0.5f,
                    bounds.center.y + bounds.extents.y * 0.4f, 0f));
            }
        }
    }

    private void OnDestroy()
    {
        if (fxRoot != null)
            Destroy(fxRoot.gameObject);
    }
}
