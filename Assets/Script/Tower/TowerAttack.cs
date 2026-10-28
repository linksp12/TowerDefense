using System.Collections.Generic;
using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [Header("Balance Data")]
    [Tooltip("연결하면 기본 공격 수치와 설치 비용을 이 데이터에서 읽습니다.")]
    public TowerData towerData;

    // 전투 중 변하는 현재 수치입니다. 저장 원본은 TowerData이며 프리팹에는 직렬화하지 않습니다.
    [System.NonSerialized] public float attackRange;
    [System.NonSerialized] public float attackCooldown;
    [System.NonSerialized] public int damage;
    [System.NonSerialized] public TowerData.ProjectileEffectStats projectileEffects;

    // 공격 대상 우선순위(설치 때 데이터 기본값, 업그레이드 창에서 바꾼다)
    [System.NonSerialized] public TargetPriority targetPriority = TargetPriority.First;

    // 맵 기믹(눈보라)이 모든 타워에 거는 사거리 배율
    public static float EnvironmentRangeMultiplier = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEnvironmentRange() => EnvironmentRangeMultiplier = 1f;

    public float EffectiveRange => attackRange * EnvironmentRangeMultiplier;
    public DamageKind DamageKind => towerData != null ? towerData.damageKind : DamageKind.Physical;

    // 용암 분출 등으로 잠깐 공격을 멈춘 상태
    private float overheatedUntil = -1f;
    public bool IsOverheated => Time.time < overheatedUntil;

    public void Overheat(float duration)
    {
        if (duration <= 0f)
            return;
        if (!IsOverheated)
            specialAttack?.Reset();
        overheatedUntil = Mathf.Max(overheatedUntil, Time.time + duration);
    }

    [Header("Stealth Detection")]
    public bool canDetectStealth = false;

    [Header("Critical Settings")]
    [Range(0f, 1f)]
    public float criticalChance = 0.1f;

    [Min(1f)]
    public float criticalDamageMultiplier = 2f;

    [Header("Projectile")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private float attackTimer = 0f;
    private TowerSpecialAttack specialAttack;
    public TowerAttackProfile CurrentAttackProfile { get; private set; }
    public float ChargeNormalized => specialAttack != null ? specialAttack.ChargeNormalized : 0;
    public Vector3 ShotOrigin => firePoint != null ? firePoint.position : transform.position;

    // 스킬 등 외부 효과가 거는 공격 속도 배율입니다. 쿨타임 값(연구 보정 포함)은 그대로 두고
    // 타이머가 흐르는 속도만 바꾸므로 능력치 UI와 연구 보정에 영향을 주지 않습니다.
    private static readonly List<TowerAttack> activeTowers = new List<TowerAttack>();
    public static IReadOnlyList<TowerAttack> ActiveTowers => activeTowers;
    private readonly TowerAttackSpeedBuffs speedBuffs = new TowerAttackSpeedBuffs();
    private readonly Dictionary<string, GameObject> buffMarkers = new Dictionary<string, GameObject>();
    private readonly List<string> expiredMarkers = new List<string>();
    public float CurrentAttackSpeedMultiplier => speedBuffs.Tick(Time.time);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTowerRegistry() => activeTowers.Clear();

    private void OnEnable()
    {
        if (!activeTowers.Contains(this)) activeTowers.Add(this);
    }

    private void OnDisable()
    {
        specialAttack?.Reset();
        activeTowers.Remove(this);
        speedBuffs.Clear();
        foreach (GameObject marker in buffMarkers.Values)
            if (marker != null) Destroy(marker);
        buffMarkers.Clear();
    }

    // Different IDs add their bonuses; sources of the same ID share one non-stacking group.
    public void ApplyAttackSpeedBuff(string buffId, object source, float bonus, float duration,
        GameObject markerPrefab = null, float markerHeight = 0.8f)
    {
        if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(buffId) || source == null ||
            duration <= 0f || bonus <= 0f || float.IsNaN(bonus) || float.IsInfinity(bonus)) return;
        speedBuffs.Set(buffId, source, bonus, Time.time + duration, Time.time);
        if (!speedBuffs.ContainsGroup(buffId) || markerPrefab == null) return;
        if (buffMarkers.TryGetValue(buffId, out GameObject existing) && existing != null) return;
        GameObject marker = Instantiate(markerPrefab, transform);
        Vector3 scale = transform.lossyScale;
        Vector3 prefabScale = markerPrefab.transform.localScale;
        marker.transform.localScale = new Vector3(
            prefabScale.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            prefabScale.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), prefabScale.z);
        marker.transform.position = transform.position + Vector3.up * markerHeight;
        marker.transform.rotation = Quaternion.identity;
        SpriteRenderer renderer = marker.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerID = SortingLayer.NameToID("Effects");
            renderer.sortingOrder = 100;
        }
        buffMarkers[buffId] = marker;
    }

    public void RemoveAttackSpeedBuff(object source)
    {
        speedBuffs.Remove(source);
        RefreshBuffMarkers();
    }

    private void RefreshBuffMarkers()
    {
        expiredMarkers.Clear();
        foreach (var entry in buffMarkers)
            if (!speedBuffs.ContainsGroup(entry.Key)) expiredMarkers.Add(entry.Key);
        foreach (string key in expiredMarkers)
        {
            if (buffMarkers[key] != null) Destroy(buffMarkers[key]);
            buffMarkers.Remove(key);
        }
    }

    // Compatibility entry point: one legacy group, independent of named skill buffs.
    public void ApplySpeedBuff(float multiplier, float duration, GameObject marker = null)
    {
        if (multiplier <= 0f || duration <= 0f)
        {
            if (marker != null)
                Destroy(marker);
            return;
        }

        ApplyAttackSpeedBuff("legacy_speed", this, multiplier - 1f, duration);
        if (marker != null)
        {
            if (buffMarkers.TryGetValue("legacy_speed", out GameObject previous) && previous != null)
                Destroy(previous);
            buffMarkers["legacy_speed"] = marker;
        }
    }

    // 프리팹을 직접 참조하는 UI/건설 코드도 데이터 에셋의 기본값을 사용할 수 있도록 제공합니다.
    public int BuildCost => towerData != null ? towerData.buildCost : 0;
    public int BaseDamage => towerData != null
        ? ResearchStatResolver.GetTowerDamage(towerData.id, towerData.baseStats.damage)
        : damage;
    
    public ElementType Element => towerData != null ? towerData.element : ElementType.None;

    // 툴팁 표시용: 실제 공격과 같은 환경 보정을 적용한 공격력
    public int GetDisplayDamage(int damageValue)
    {
        return EnvironmentManager.ApplyToDamage(damageValue, Element);
    }
    public float BaseAttackCooldown => towerData != null
        ? ResearchStatResolver.GetTowerAttackCooldown(towerData.id, towerData.baseStats.attackCooldown)
        : attackCooldown;
    public float BaseAttackRange => towerData != null
        ? ResearchStatResolver.GetTowerAttackRange(towerData.id, towerData.baseStats.attackRange)
        : attackRange;

    public string UpgradeRouteSummary
    {
        get
        {
            if (towerData == null)
                return "정보 없음";

            string pathAName = towerData.pathA != null ? towerData.pathA.routeName : "";
            string pathBName = towerData.pathB != null ? towerData.pathB.routeName : "";

            if (string.IsNullOrEmpty(pathAName))
                return pathBName;

            if (string.IsNullOrEmpty(pathBName))
                return pathAName;

            return $"{pathAName} / {pathBName}";
        }
    }

    private void Awake()
    {
        ApplyBaseStatsFromData();
    }

    public void ApplyBaseStatsFromData()
    {
        if (towerData == null)
        {
            Debug.LogError($"{name}: TowerData가 연결되지 않았습니다.", this);
            return;
        }

        ApplyGrowthToCombatStats(
            towerData.baseStats.damage,
            towerData.baseStats.attackCooldown,
            towerData.baseStats.attackRange);
        projectileEffects = towerData.baseProjectileEffects;
        CurrentAttackProfile = towerData.baseAttack;
        // 데이터에서 은신 감지를 켜면 프리팹 설정과 상관없이 적용한다(도감 표시와 같은 값).
        if (towerData.detectsStealth)
            canDetectStealth = true;
        targetPriority = towerData.defaultTargetPriority;
        specialAttack?.Reset();
    }

    private void Update()
    {
        RefreshBuffMarkers();
        if (IsOverheated)
            return;
        attackTimer += Time.deltaTime * CurrentAttackSpeedMultiplier;

        if (CurrentAttackProfile != null && CurrentAttackProfile.mode != TowerAttackMode.Projectile)
        {
            if (specialAttack == null) specialAttack = new TowerSpecialAttack(this);
            specialAttack.Tick(Time.deltaTime * CurrentAttackSpeedMultiplier);
            return;
        }

        GameObject target = FindTarget();

        if (target != null &&
            attackTimer >= attackCooldown)
        {
            Attack(target);
            attackTimer = 0f;
        }
    }

    // =========================================================
    // 공격 대상 찾기(타워마다 정한 우선순위: 선두·후미·강한 적·가까운 적)
    // =========================================================
    private GameObject FindTarget()
    {
        // 폭발 공격(캐논)은 하늘을 나는 몬스터를 맞히지 못한다. 몬스터 태그가 붙은 적만 노린다.
        bool explodes = projectileEffects.canExplode;
        MonsterHealth target = TowerTargeting.Select(this, targetPriority, null,
            enemy => enemy.CompareTag("Monster") && !(explodes && enemy.GetComponent<FlyingMonster>() != null));
        return target != null ? target.gameObject : null;
    }

    // =========================================================
    // 공격
    // =========================================================
    private void Attack(GameObject target)
    {
        if (target == null)
            return;

        // 공격 직전 다시 사망 여부 확인
        MonsterHealth targetHealth =
            target.GetComponent<MonsterHealth>();

        if (targetHealth == null)
            return;

        if (targetHealth.IsDead)
            return;

        // 화살 프리팹 확인
        if (arrowPrefab == null)
        {
            Debug.LogWarning(
                "Arrow Prefab이 연결되지 않았습니다."
            );

            return;
        }

        Vector3 spawnPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;

        // 화살 생성
        GameObject arrow =
            Instantiate(
                arrowPrefab,
                spawnPosition,
                Quaternion.identity
            );

        // ArrowProjectile 가져오기
        ArrowProjectile projectile =
            arrow.GetComponent<ArrowProjectile>();

        TowerData.AttackFeel feel = towerData != null ? towerData.attackFeel : TowerData.AttackFeel.Arrow;

        if (projectile != null)
        {
            projectile.SetTarget(
                target.transform,
                damage,
                canDetectStealth,
                criticalChance,
                criticalDamageMultiplier
            );
            projectile.ApplyEffectStats(projectileEffects);
            projectile.SetFeel(feel);
            projectile.SetElement(Element);
            projectile.SetDamageKind(DamageKind);
        }
        else
        {
            Debug.LogWarning(
                "Arrow Prefab에 ArrowProjectile이 없습니다."
            );
        }

        // 발사음과 발사 연출 (타워 데이터의 공격 연출 종류 기준)
        CombatFeedback.TowerFired(feel, spawnPosition);
    }

    // =========================================================
    // 사거리 표시
    // =========================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            EffectiveRange
        );
    }

    // =========================================================
    // 업그레이드 능력치 적용
    // =========================================================
    public void ApplyUpgradeStats(
        int newDamage,
        float newCooldown,
        float newRange,
        GameObject newArrowPrefab,
        TowerData.ProjectileEffectStats newProjectileEffects)
    {
        ApplyGrowthToCombatStats(newDamage, newCooldown, newRange);
        projectileEffects = newProjectileEffects;
        var upgrade = GetComponent<TowerUpgrade>();
        CurrentAttackProfile = towerData != null ? towerData.GetAttackProfile(
            upgrade != null ? upgrade.path : TowerUpgrade.UpgradePath.None,
            upgrade != null ? upgrade.level : 1) : null;
        specialAttack?.Reset();

        if (newArrowPrefab != null)
        {
            arrowPrefab = newArrowPrefab;
        }

        Debug.Log(
            "타워 능력치 변경 완료 / 공격력: " +
            damage +
            " / 쿨타임: " +
            attackCooldown +
            " / 사거리: " +
            attackRange
        );
    }

    private void ApplyGrowthToCombatStats(int baseDamage, float baseCooldown, float baseRange)
    {
        string towerId = towerData != null ? towerData.id : string.Empty;
        damage = ResearchStatResolver.GetTowerDamage(towerId, baseDamage);
        attackCooldown = ResearchStatResolver.GetTowerAttackCooldown(towerId, baseCooldown);
        attackRange = ResearchStatResolver.GetTowerAttackRange(towerId, baseRange);
    }
}
