using UnityEngine;

/// <summary>Fits the artwork's circular boundary to the combat radius, including parent scale.</summary>
internal static class SkillRangeVisual
{
    public static void Fit(SpriteRenderer renderer, float radius, float ringRadiusFraction)
    {
        if (renderer == null || renderer.sprite == null || radius <= 0f) return;
        Vector3 size = renderer.sprite.bounds.size;
        Vector3 parentScale = renderer.transform.parent != null
            ? renderer.transform.parent.lossyScale : Vector3.one;
        float diameter = radius * 2f / Mathf.Clamp(ringRadiusFraction, 0.1f, 1f);
        renderer.transform.localScale = new Vector3(
            diameter / Mathf.Max(0.0001f, size.x * Mathf.Abs(parentScale.x)),
            diameter / Mathf.Max(0.0001f, size.y * Mathf.Abs(parentScale.y)), 1f);
    }
}
