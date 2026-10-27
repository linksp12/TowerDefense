using UnityEngine;

/// <summary>전투 판정과 분리된 스킬 연출 생성·크기 맞춤.</summary>
internal static class SkillVisuals
{
    public static SpriteRenderer MainRenderer(Component target)
    {
        SpriteRenderer main = target.GetComponent<SpriteRenderer>();
        return main != null ? main : target.GetComponentInChildren<SpriteRenderer>();
    }

    public static void FitWidth(SpriteRenderer renderer, float width)
    {
        if (renderer == null || renderer.sprite == null) return;
        Vector3 parent = renderer.transform.parent != null ? renderer.transform.parent.lossyScale : Vector3.one;
        float scale = width / Mathf.Max(0.001f, renderer.sprite.bounds.size.x);
        renderer.transform.localScale = new Vector3(scale / Mathf.Max(0.001f, Mathf.Abs(parent.x)),
            scale / Mathf.Max(0.001f, Mathf.Abs(parent.y)), 1);
    }

    public static SpriteRenderer Create(string name, Transform parent = null)
    {
        GameObject go = new GameObject(name, typeof(SpriteRenderer));
        if (parent != null) go.transform.SetParent(parent, false);
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sortingLayerID = SortingLayer.NameToID("Effects");
        renderer.sortingOrder = 100;
        return renderer;
    }

    public static void PlayCast(SkillData skill, Vector3 position, float radius)
    {
        SkillVisualProfile profile = skill.VisualProfile;
        if (profile == null || profile.castFrames == null || profile.castFrames.Length == 0) return;
        if (float.IsInfinity(radius)) radius = skill.Range;
        SpriteRenderer visual = Create(skill.SkillId + " Cast");
        visual.transform.position = position;
        visual.sprite = profile.castFrames[0];
        float width = skill.EffectType == SkillEffectType.Lightning ? 1.8f : radius * 2 * profile.castDiameterMultiplier;
        FitWidth(visual, width);
        visual.gameObject.AddComponent<SkillSpriteEffect>().Initialize(profile.castFrames, profile.castDuration);
    }

    public static void AttachStatus(MonsterHealth target, SkillData skill, float duration)
    {
        if (target == null || target.IsDead || skill.VisualProfile == null || duration <= 0) return;
        SkillStatusVisual status = null;
        foreach (SkillStatusVisual candidate in target.GetComponentsInChildren<SkillStatusVisual>())
            if (candidate.SkillId == skill.SkillId) { status = candidate; break; }
        if (status == null)
        {
            SpriteRenderer renderer = Create(skill.SkillId + " Status", target.transform);
            status = renderer.gameObject.AddComponent<SkillStatusVisual>();
        }
        status.Refresh(target, skill, duration);
    }

    public static SpriteRenderer CreateSilhouette(SpriteRenderer source, SkillVisualProfile profile, Transform parent)
    {
        if (source == null || profile.silhouetteMaterial == null) return null;
        SpriteRenderer outline = Create("Warm Outline", parent);
        outline.sharedMaterial = profile.silhouetteMaterial;
        outline.sortingLayerID = source.sortingLayerID;
        outline.sortingOrder = source.sortingOrder - 1;
        return outline;
    }

    public static void FollowSilhouette(SpriteRenderer outline, SpriteRenderer source, Color color)
    {
        if (outline == null || source == null) return;
        outline.sprite = source.sprite;
        outline.flipX = source.flipX; outline.flipY = source.flipY;
        outline.color = color;
        outline.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        Vector3 parent = outline.transform.parent.lossyScale;
        Vector3 scale = source.transform.lossyScale * 1.06f;
        outline.transform.localScale = new Vector3(scale.x / parent.x, scale.y / parent.y, 1);
    }
}
