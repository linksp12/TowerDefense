using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화염 드래곤 보스: 체력 구간마다 한 번씩 브레스로 타워를 부숩니다.
/// - 2초 동안 숨을 들이쉬며 바닥에 경고 원을 띄웁니다. 그동안 최대 체력의 일정 비율 이상 맞으면 브레스가 끊깁니다.
/// - 경고 원 안의 타워를 최대 몇 개 부수고, 쓴 골드의 일부를 돌려줍니다. 자리에는 불탄 자국이 남고 다시 지을 수 있습니다.
/// </summary>
public class FireDragonBreath : MonoBehaviour
{
    [Header("발동 (체력 비율)")]
    [SerializeField] private float[] hpThresholds = { 0.7f, 0.4f };

    [Header("예고")]
    [SerializeField, Min(0.5f)] private float chargeTime = 2f;
    [SerializeField, Range(0.01f, 0.5f)] private float interruptDamageRatio = 0.06f;
    [SerializeField] private Sprite warningSprite;
    [SerializeField, Min(0.5f)] private float warningRadius = 2f;

    [Header("브레스")]
    [SerializeField, Min(1f)] private float range = 7f;
    [SerializeField, Min(1)] private int maxTowers = 2;
    [SerializeField, Range(0f, 1f)] private float refundRate = 0.5f;
    [SerializeField] private Sprite scorchSprite;
    [SerializeField] private Vector2 mouthOffset = new Vector2(2f, 0.5f);

    [Header("Animator State")]
    [SerializeField] private string chargeState = "fire_dragon_charge";
    [SerializeField] private string breathState = "fire_dragon_breath";
    [SerializeField] private string walkState = "fire_dragon_walk";

    private MonsterHealth health;
    private MonsterMove move;
    private Animator animator;
    private FaceMoveDirection facing;
    private bool[] used;
    private bool busy;
    private int pending;

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
        move = GetComponent<MonsterMove>();
        animator = GetComponent<Animator>();
        facing = GetComponent<FaceMoveDirection>();
        used = new bool[hpThresholds.Length];
    }

    private void OnEnable()
    {
        if (health != null)
            health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Damaged -= OnDamaged;
    }

    private void OnDamaged(MonsterHealth monster, int damage)
    {
        for (int i = 0; i < hpThresholds.Length; i++)
        {
            if (!used[i] && monster.CurrentHp <= monster.maxHp * hpThresholds[i])
            {
                used[i] = true;
                pending++;
            }
        }

        if (pending > 0 && !busy)
            StartCoroutine(BreathRoutine());
    }

    private IEnumerator BreathRoutine()
    {
        busy = true;

        while (pending > 0 && health != null && !health.IsDead)
        {
            pending--;
            yield return Breath();
        }

        busy = false;
    }

    private IEnumerator Breath()
    {
        if (!TryPickTarget(out Vector3 center))
            yield break;

        if (move != null)
            move.enabled = false;
        Play(chargeState);

        SpriteRenderer warning = CreateWarning(center);
        int startHp = health.CurrentHp;
        float t = 0f;
        bool interrupted = false;

        while (t < chargeTime)
        {
            t += Time.deltaTime;

            if (warning != null)
            {
                // 끝에 가까울수록 빠르게 깜빡인다.
                float speed = Mathf.Lerp(6f, 22f, t / chargeTime);
                Color c = warning.color;
                c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * speed));
                warning.color = c;
            }

            if (health == null || health.IsDead)
            {
                interrupted = true;
                break;
            }

            if (startHp - health.CurrentHp >= health.maxHp * interruptDamageRatio)
            {
                interrupted = true;
                DamagePopup.ShowShield(transform.position + Vector3.up);
                MonsterAbilityUtil.Shake(0.25f);
                break;
            }

            yield return null;
        }

        if (warning != null)
            Destroy(warning.gameObject);

        if (!interrupted)
        {
            Play(breathState);
            BreatheFire(center);
            yield return new WaitForSeconds(0.45f);
        }

        if (health != null && !health.IsDead)
        {
            if (move != null)
                move.enabled = true;
            Play(walkState);
        }
    }

    // 범위 안에서 경고 원에 타워가 가장 많이 들어가는 타워 위치를 고른다.
    private bool TryPickTarget(out Vector3 center)
    {
        center = Vector3.zero;
        List<TowerAttack> towers = MonsterAbilityUtil.FindNearbyTowers(transform.position, range, int.MaxValue);
        if (towers.Count == 0)
            return false;

        int best = -1;
        foreach (TowerAttack candidate in towers)
        {
            int count = 0;
            foreach (TowerAttack other in towers)
            {
                if (Vector3.Distance(candidate.transform.position, other.transform.position) <= warningRadius)
                    count++;
            }

            if (count > best)
            {
                best = count;
                center = candidate.transform.position;
            }
        }

        return true;
    }

    private void BreatheFire(Vector3 center)
    {
        Vector3 mouth = MouthPosition();

        // 입에서 목표까지 불길
        for (int i = 1; i <= 6; i++)
        {
            Vector3 p = Vector3.Lerp(mouth, center, i / 6f);
            CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, p, false, 0f, true, false);
        }

        CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Cannon, center, true, warningRadius, false, false);
        MonsterAbilityUtil.Shake(0.6f);

        List<TowerAttack> hit = MonsterAbilityUtil.FindNearbyTowers(center, warningRadius, maxTowers);
        foreach (TowerAttack tower in hit)
            DestroyTower(tower);
    }

    private void DestroyTower(TowerAttack tower)
    {
        Vector3 pos = tower.transform.position;
        TowerUpgrade upgrade = tower.GetComponent<TowerUpgrade>();

        int refund = upgrade != null ? Mathf.RoundToInt(upgrade.GetTotalUsedCost() * refundRate) : 0;
        if (refund > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(refund);
            DamagePopup.ShowGold(pos, refund);
        }

        // 판매와 같은 순서: 설치 지점 비우기 → 열려 있는 강화 창 닫기 → 타워 제거
        BuildPoint buildPoint = upgrade != null ? upgrade.GetOwnerBuildPoint() : null;
        if (buildPoint != null)
            buildPoint.ClearTower();

        if (TowerUpgradeUI.Instance != null)
            TowerUpgradeUI.Instance.CloseInstantly();

        SpawnScorch(pos);
        Destroy(tower.gameObject);
    }

    private SpriteRenderer CreateWarning(Vector3 center)
    {
        if (warningSprite == null)
            return null;

        GameObject go = new GameObject("BreathWarning");
        go.transform.position = center;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = warningSprite;
        sr.sortingLayerName = "Towers";
        sr.sortingOrder = 20;
        go.transform.localScale = Vector3.one * (warningRadius * 2f / warningSprite.bounds.size.x);
        return sr;
    }

    private void SpawnScorch(Vector3 pos)
    {
        if (scorchSprite == null)
            return;

        GameObject go = new GameObject("BreathScorch");
        go.transform.position = pos + new Vector3(0f, -0.4f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = scorchSprite;
        sr.sortingLayerName = "Towers";
        sr.sortingOrder = 0;
        go.transform.localScale = Vector3.one * 1.6f;
        go.AddComponent<FadeAndDestroy>().Init(6f, 2f);
    }

    private Vector3 MouthPosition()
    {
        float side = facing != null && facing.FacingLeft ? -1f : 1f;
        return transform.position + new Vector3(mouthOffset.x * side, mouthOffset.y, 0f);
    }

    private void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state))
            animator.Play(state, 0, 0f);
    }
}
