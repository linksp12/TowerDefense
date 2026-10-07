using UnityEngine;

public enum SkillEffectType
{
    [InspectorName("불")]
    Fire,
    [InspectorName("얼음")]
    Ice,
    [InspectorName("번개")]
    Lightning,
    [InspectorName("타워 가속")]
    TowerHaste
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

    [Header("타워 강화 (타워 가속 전용)")]
    [InspectorName("공격 속도 증가율")]
    [Tooltip("0.3이면 범위 안 타워의 공격 속도가 30% 빨라집니다. 지속 시간은 위의 '지속 시간'을 사용합니다.")]
    [Min(0f)]
    [SerializeField] private float attackSpeedBonus = 0.3f;
    [InspectorName("타워 버프 표시 프리팹")]
    [SerializeField] private GameObject buffMarkerPrefab;
    [InspectorName("버프 표시 높이")]
    [Tooltip("타워 중심에서 버프 표시까지의 높이(월드 단위)입니다.")]
    [SerializeField] private float buffMarkerHeight = 0.8f;

    [Tooltip("조준 이미지에서 실제 원 테두리 반경 / 이미지 반경입니다. 여백을 제외하고 범위를 맞춥니다.")]
    [Range(0.1f, 1f)] [SerializeField] private float magicCircleRadiusFraction = 1f;
    [Tooltip("지속 장판 이미지의 실제 원 테두리 반경 / 이미지 반경입니다.")]
    [Range(0.1f, 1f)] [SerializeField] private float effectRadiusFraction = 1f;

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
    public float AttackSpeedBonus => attackSpeedBonus;
    public GameObject BuffMarkerPrefab => buffMarkerPrefab;
    public float BuffMarkerHeight => buffMarkerHeight;
    public float MagicCircleRadiusFraction => magicCircleRadiusFraction;
    public float EffectRadiusFraction => effectRadiusFraction;
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
        attackSpeedBonus = Mathf.Max(0f, attackSpeedBonus);
        magicCircleRadiusFraction = Mathf.Clamp(magicCircleRadiusFraction, 0.1f, 1f);
        effectRadiusFraction = Mathf.Clamp(effectRadiusFraction, 0.1f, 1f);
        effectScale = Mathf.Max(0.01f, effectScale);
    }
}
