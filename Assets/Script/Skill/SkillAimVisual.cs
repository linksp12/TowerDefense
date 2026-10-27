using System;
using UnityEngine;

internal struct SkillAimVisualSettings
{
    public Color FireColor, IceColor, LightningColor, HasteColor;
    public float CircleScale, FireScale, IceScale, LightningScale;
    public float FollowSpeed, RotationSpeed, NormalAlpha, HighlightAlpha, HighlightSpeed, LineWidth;
    public bool Rotate, Highlight;
    public int Segments;
    public LayerMask MonsterLayer;
}

/// <summary>마법진과 범위 선의 표시만 담당합니다. 입력, 쿨타임, 스킬 발동은 처리하지 않습니다.</summary>
internal sealed class SkillAimVisual : IDisposable
{
    private readonly GameObject circle;
    private readonly GameObject indicator;
    private readonly SpriteRenderer spriteRenderer;
    private readonly Vector3 originalSpriteScale;
    private readonly LineRenderer line;
    private readonly Material material;
    private Collider2D[] overlapBuffer = new Collider2D[16];
    private Vector3[] circlePoints;
    private float drawnRadius = -1f;
    private Color drawnColor;
    private bool hasDrawnColor;
    private float drawnWidth = -1f;
    private SkillEffectType effectType;

    public bool IsReady => circle != null && spriteRenderer != null;

    public SkillAimVisual(GameObject circle, GameObject indicator)
    {
        this.circle = circle;
        this.indicator = indicator;
        if (circle != null)
        {
            spriteRenderer = circle.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = circle.GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null)
            {
                originalSpriteScale = spriteRenderer.transform.localScale;
                spriteRenderer.sortingLayerID = SortingLayer.NameToID("Effects");
                spriteRenderer.sortingOrder = 6;
            }
        }
        if (indicator != null)
        {
            line = indicator.GetComponent<LineRenderer>();
            if (line == null) line = indicator.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.sortingLayerID = SortingLayer.NameToID("Effects");
            line.sortingOrder = 5;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader) { name = "SkillRangeLineMaterial" };
                line.sharedMaterial = material;
            }
        }
        Hide();
    }

    public bool Show(SkillData skill, Vector3 position, float radius, SkillAimVisualSettings settings)
    {
        if (!IsReady || skill == null || skill.MagicCircle == null) return false;
        effectType = skill.EffectType;
        spriteRenderer.sprite = skill.MagicCircle;
        spriteRenderer.enabled = true;
        if (spriteRenderer.transform != circle.transform)
            spriteRenderer.transform.localScale = originalSpriteScale;
        float scale = settings.CircleScale;
        switch (effectType)
        {
            case SkillEffectType.Fire: scale *= settings.FireScale; break;
            case SkillEffectType.Ice: scale *= settings.IceScale; break;
            case SkillEffectType.Lightning: scale *= settings.LightningScale; break;
        }
        circle.transform.localScale = Vector3.one * scale;
        if (effectType == SkillEffectType.TowerHaste)
            SkillRangeVisual.Fit(spriteRenderer, radius, skill.MagicCircleRadiusFraction);
        circle.transform.rotation = Quaternion.identity;
        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(settings.NormalAlpha);
        spriteRenderer.color = color;
        circle.SetActive(true);
        if (indicator != null) indicator.SetActive(true);
        SetPosition(position);
        UpdateLine(radius, settings);
        return true;
    }

    public void Tick(Vector3 targetPosition, float radius, SkillAimVisualSettings settings)
    {
        if (!IsReady) return;
        float follow = 1f - Mathf.Exp(-settings.FollowSpeed * Time.unscaledDeltaTime);
        circle.transform.position = Vector3.Lerp(circle.transform.position, targetPosition, follow);
        if (settings.Rotate) circle.transform.Rotate(0f, 0f, settings.RotationSpeed * Time.unscaledDeltaTime);
        if (indicator != null)
        {
            indicator.transform.position = circle.transform.position;
            indicator.transform.rotation = Quaternion.identity;
        }
        UpdateLine(radius, settings);
        float targetAlpha = settings.Highlight && HasTargetInside(radius, settings.MonsterLayer)
            ? settings.HighlightAlpha : settings.NormalAlpha;
        if (effectType == SkillEffectType.TowerHaste) targetAlpha = Mathf.Min(targetAlpha, 0.6f);
        Color color = spriteRenderer.color;
        color.a = settings.Highlight
            ? Mathf.Lerp(color.a, targetAlpha, settings.HighlightSpeed * Time.unscaledDeltaTime)
            : Mathf.Clamp01(settings.NormalAlpha);
        spriteRenderer.color = color;
    }

    public void SetPosition(Vector3 position)
    {
        if (circle != null) circle.transform.position = position;
        if (indicator != null)
        {
            indicator.transform.position = position;
            indicator.transform.rotation = Quaternion.identity;
        }
    }

    private void UpdateLine(float radius, SkillAimVisualSettings settings)
    {
        if (line == null || radius <= 0f) return;
        int segments = Mathf.Clamp(settings.Segments, 32, 128);
        // 마우스 이동 중에는 좌표만 옮기고, 반경·정밀도가 달라질 때만 원을 다시 계산합니다.
        if (circlePoints == null || circlePoints.Length != segments || radius != drawnRadius)
        {
            if (circlePoints == null || circlePoints.Length != segments) circlePoints = new Vector3[segments];
            for (int index = 0; index < segments; index++)
            {
                float angle = (360f / segments) * index * Mathf.Deg2Rad;
                circlePoints[index] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }
            line.positionCount = segments;
            line.SetPositions(circlePoints);
            drawnRadius = radius;
        }
        Color color = settings.FireColor;
        if (effectType == SkillEffectType.Ice) color = settings.IceColor;
        else if (effectType == SkillEffectType.Lightning) color = settings.LightningColor;
        else if (effectType == SkillEffectType.TowerHaste) color = settings.HasteColor;
        if (!hasDrawnColor || color != drawnColor)
        {
            line.startColor = line.endColor = color;
            if (material != null) material.color = color;
            drawnColor = color;
            hasDrawnColor = true;
        }
        if (settings.LineWidth != drawnWidth)
        {
            line.startWidth = line.endWidth = settings.LineWidth;
            drawnWidth = settings.LineWidth;
        }
        line.enabled = true;
    }

    private bool HasTargetInside(float radius, LayerMask monsterLayer)
    {
        if (effectType != SkillEffectType.TowerHaste) return HasEnemyInside(radius, monsterLayer);
        if (circle == null || radius <= 0f) return false;
        foreach (TowerAttack tower in TowerAttack.ActiveTowers)
            if (tower != null && tower.isActiveAndEnabled && tower.towerData != null &&
                ((Vector2)(tower.transform.position - circle.transform.position)).sqrMagnitude <= radius * radius)
                return true;
        return false;
    }

    private bool HasEnemyInside(float radius, LayerMask monsterLayer)
    {
        if (circle == null || radius <= 0f) return false;
        ContactFilter2D filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        filter.SetLayerMask(monsterLayer.value != 0 ? monsterLayer.value : Physics2D.AllLayers);
        while (true)
        {
            int count = Physics2D.OverlapCircle(circle.transform.position, radius, filter, overlapBuffer);
            for (int index = 0; index < count; index++)
            {
                Collider2D hit = overlapBuffer[index];
                if (hit == null) continue;
                MonsterHealth monster = hit.GetComponentInParent<MonsterHealth>();
                if (monster != null && !monster.IsDead) return true;
            }
            if (count < overlapBuffer.Length) return false;
            // 비몬스터 Collider가 많아도 실제 몬스터를 누락하지 않도록 필요할 때만 버퍼를 늘립니다.
            Array.Resize(ref overlapBuffer, overlapBuffer.Length * 2);
        }
    }

    public void Hide()
    {
        if (circle != null) circle.SetActive(false);
        if (line != null) line.enabled = false;
        if (indicator != null) indicator.SetActive(false);
    }

    public void Dispose()
    {
        Hide();
        if (material != null) UnityEngine.Object.Destroy(material);
    }
}
