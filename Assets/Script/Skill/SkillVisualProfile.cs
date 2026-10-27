using UnityEngine;

/// <summary>스킬의 그림·재생 속도만 보관한다. 피해와 버프 수치는 SkillData가 소유한다.</summary>
[CreateAssetMenu(fileName = "SkillVisuals", menuName = "Last of the Tower/Skill Visuals")]
public sealed class SkillVisualProfile : ScriptableObject
{
    public Sprite[] castFrames;
    public Sprite[] statusFrames;
    public Sprite[] targetFrames;
    [Header("조준 대상 표시")]
    [Min(0.1f)] public float targetWidthMultiplier = 1.15f;
    public Sprite targetHeadMarker;
    [Min(0.1f)] public float targetHeadWidthMultiplier = 0.45f;
    [Min(0f)] public float targetHeadGap = 0.15f;
    [Min(0.05f)] public float castDuration = 0.4f;
    [Min(1)] public float statusFps = 7f;
    [Min(1)] public float targetFps = 6f;
    [Range(0.1f, 1.5f)] public float castDiameterMultiplier = 0.85f;
    public Color accent = Color.white;
    public Material silhouetteMaterial;
}
