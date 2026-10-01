using UnityEngine;

public enum SkillEffectType
{
    [InspectorName("불")]
    Fire,
    [InspectorName("얼음")]
    Ice,
    [InspectorName("번개")]
    Lightning
}

[CreateAssetMenu(
    fileName = "NewSkillData",
    menuName = "Last of the Tower/Skill Data")]
public sealed class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    [InspectorName("스킬 ID")]
    [SerializeField] private string skillId;
    [InspectorName("스킬 이름")]
    [SerializeField] private string displayName;
    [InspectorName("설명")]
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [InspectorName("아이콘")]
    [SerializeField] private Sprite icon;
    [InspectorName("스킬 종류")]
    [SerializeField] private SkillEffectType effectType;

    [Header("전투 수치")]
    [InspectorName("피해량")]
    [Min(0)]
    [SerializeField] private int damage;
    [InspectorName("공격 범위")]
    [Min(0f)]
    [SerializeField] private float range = 1f;
    [InspectorName("재사용 대기시간")]
    [Min(0f)]
    [SerializeField] private float cooldown;
    [InspectorName("지속 시간")]
    [Min(0f)]
    [SerializeField] private float duration;
    [InspectorName("최대 공격 대상 수")]
    [Min(0)]
    [SerializeField] private int maxTargets;

    [Header("지속 효과")]
    [InspectorName("지속 피해량")]
    [Min(0)]
    [SerializeField] private int periodicDamage;
    [InspectorName("지속 피해 간격")]
    [Min(0.05f)]
    [SerializeField] private float periodicInterval = 0.5f;

    [Header("연출 및 사운드")]
    [InspectorName("마법진 이미지")]
    [SerializeField] private Sprite magicCircle;
    [InspectorName("이펙트 프리팹")]
    [SerializeField] private GameObject effectPrefab;
    [InspectorName("이펙트 크기 배율")]
    [Min(0.01f)]
    [SerializeField] private float effectScale = 1f;
    [InspectorName("효과음")]
    [SerializeField] private AudioClip sound;
    [InspectorName("기본 효과음 음량")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.7f;

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public SkillEffectType EffectType => effectType;
    public int Damage => damage;
    public float Range => range;
    public float Cooldown => cooldown;
    public float Duration => duration;
    public int MaxTargets => maxTargets;
    public int PeriodicDamage => periodicDamage;
    public float PeriodicInterval => periodicInterval;
    public Sprite MagicCircle => magicCircle;
    public GameObject EffectPrefab => effectPrefab;
    public float EffectScale => effectScale;
    public AudioClip Sound => sound;
    public float SoundVolume => soundVolume;

    public bool Matches(string idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName))
            return false;

        return string.Equals(skillId, idOrName, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(displayName, idOrName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void OnValidate()
    {
        skillId = skillId?.Trim().ToLowerInvariant().Replace(' ', '_');
        displayName = displayName?.Trim();
        range = Mathf.Max(0f, range);
        cooldown = Mathf.Max(0f, cooldown);
        duration = Mathf.Max(0f, duration);
        periodicInterval = Mathf.Max(0.05f, periodicInterval);
        effectScale = Mathf.Max(0.01f, effectScale);
    }
}
