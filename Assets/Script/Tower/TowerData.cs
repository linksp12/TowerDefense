using UnityEngine;

/// <summary>
/// 타워의 밸런스 수치와 표시 텍스트(이름·설명)를 보관하는 공용 데이터 에셋입니다.
/// 프리팹은 외형(스프라이트·발사체)을, 이 에셋은 수치와 텍스트를 담당합니다.
/// </summary>
[CreateAssetMenu(fileName = "TowerData", menuName = "TowerDefense/Tower Data")]
public class TowerData : ScriptableObject
{
    [System.Serializable]
    public struct CombatStats
    {
        public int damage;
        public float attackCooldown;
        public float attackRange;
    }

    [System.Serializable]
    public struct ProjectileEffectStats
    {
        [Header("관통")]
        public bool canPierce;
        [Min(1)] public int maxHitCount;

        [Header("폭발")]
        public bool canExplode;
        [Min(0f)] public float explosionRadius;
        [Range(0f, 1f)] public float splashDamageRate;

        [Header("슬로우")]
        public bool canSlow;
        [Range(0f, 1f)] public float slowRate;
        [Min(0f)] public float slowDuration;

        [Header("지속 피해")]
        public bool canDot;
        [Min(0)] public int dotDamage;
        [Min(0f)] public float dotDuration;
        [Min(0f)] public float dotInterval;
    }

    [System.Serializable]
    public class PathTextInfo
    {
        [Tooltip("업그레이드 루트 선택 버튼에 표시되는 이름 (예: 연사 루트)")]
        public string routeName;

        [Tooltip("이 루트로 Lv.2가 되었을 때 타워 이름")]
        public string lv2Name;

        [Tooltip("이 루트로 Lv.3(최종)이 되었을 때 타워 이름")]
        public string lv3Name;

        [Tooltip("루트 선택 화면에 표시되는 설명")]
        [TextArea]
        public string featureText;

        [Tooltip("최종 업그레이드(Lv.3) 안내에 표시되는 효과 설명")]
        [TextArea]
        public string effectText;
    }

    [Header("기본 정보")]
    [Tooltip("카탈로그와 코드에서 사용하는 고유 ID (예: arrow_tower)")]
    public string id;
    public string towerName;

    [TextArea]
    public string description;

    [Tooltip("타워 카드 등 UI에서 사용할 기본 아이콘입니다.")]
    public Sprite icon;

    public int buildCost;
    [Range(0f, 1f)] public float sellRate = 0.7f;

    [Header("기본 능력치")]
    public CombatStats baseStats;

    [Header("기본 투사체 효과")]
    public ProjectileEffectStats baseProjectileEffects;

    [Header("업그레이드 비용")]
    public int level2Cost;
    public int level3Cost;

    [Header("A 경로 능력치")]
    public CombatStats pathALv2Stats;
    public CombatStats pathALv3Stats;

    [Header("A 경로 투사체 효과")]
    public ProjectileEffectStats pathALv2ProjectileEffects;
    public ProjectileEffectStats pathALv3ProjectileEffects;

    [Header("B 경로 능력치")]
    public CombatStats pathBLv2Stats;
    public CombatStats pathBLv3Stats;

    [Header("B 경로 투사체 효과")]
    public ProjectileEffectStats pathBLv2ProjectileEffects;
    public ProjectileEffectStats pathBLv3ProjectileEffects;

    [Header("A 경로 텍스트")]
    public PathTextInfo pathA;

    [Header("B 경로 텍스트")]
    public PathTextInfo pathB;
}
