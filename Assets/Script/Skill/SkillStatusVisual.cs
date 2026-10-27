using UnityEngine;

/// <summary>피격 대상의 표시만 따라간다. 피해·빙결 수명은 기존 전투 코드가 결정한다.</summary>
public sealed class SkillStatusVisual : MonoBehaviour
{
    private MonsterHealth target;
    private MonsterMove movement;
    private SpriteRenderer visual, source, silhouette;
    private SkillData skill;
    private float expires;
    private Bounds frozenBounds;
    private Vector3 frozenTargetPosition;
    public string SkillId => skill != null ? skill.SkillId : null;

    public void Refresh(MonsterHealth monster, SkillData data, float duration)
    {
        target = monster; skill = data;
        expires = Mathf.Max(expires, Time.time + duration);
        visual = GetComponent<SpriteRenderer>();
        source = SkillVisuals.MainRenderer(target);
        movement = target.GetComponent<MonsterMove>();
        if (data.EffectType == SkillEffectType.Ice && source != null)
        {
            frozenBounds = source.bounds;
            frozenTargetPosition = target.transform.position;
        }
        if (data.EffectType == SkillEffectType.Fire && silhouette == null)
            silhouette = SkillVisuals.CreateSilhouette(source, data.VisualProfile, transform);
    }

    private void LateUpdate()
    {
        if (target == null || target.IsDead || source == null || !target.gameObject.activeInHierarchy ||
            Time.time >= expires || (skill.EffectType == SkillEffectType.Ice && movement != null && !movement.IsFrozen))
        { Destroy(gameObject); return; }
        SkillVisualProfile profile = skill.VisualProfile;
        if (profile.statusFrames == null || profile.statusFrames.Length == 0) return;
        visual.sprite = profile.statusFrames[(int)(Time.time * profile.statusFps) % profile.statusFrames.Length];
        Bounds body = source.bounds;
        // 빙결 껍질은 원본 애니메이션의 프레임별 크기 변화에 흔들리지 않는다.
        if (skill.EffectType == SkillEffectType.Ice)
        {
            body = frozenBounds;
            body.center += target.transform.position - frozenTargetPosition;
        }
        SkillVisuals.FitWidth(visual, body.size.x * (skill.EffectType == SkillEffectType.Fire ? 1.35f : 1.2f));
        transform.position = new Vector3(body.center.x, body.min.y, body.center.z);
        Color tint = profile.accent; tint.a = 0.65f;
        SkillVisuals.FollowSilhouette(silhouette, source, tint);
    }
}
