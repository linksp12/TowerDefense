using System.Collections;
using UnityEngine;

public class MonsterMove : MonoBehaviour
{
    public MonsterData monsterData;

    public Transform[] waypoints;
    [HideInInspector] public float moveSpeed;

    private Coroutine slowCoroutine;
    
    private Coroutine freezeCoroutine;

    private int currentWaypointIndex = 0;
    private bool hasReachedShield = false;

    // 스킬용 빙결 상태
    private bool isFrozen = false;

    private int baseDamage;
    private readonly MonsterControlEffects controlEffects = new MonsterControlEffects();

    public bool IsShockStunned => controlEffects.IsStunned;
    public float EffectiveMoveSpeed => controlEffects.GetSpeed(moveSpeed,
        monsterData != null ? monsterData.moveSpeed : moveSpeed);

    public void ApplyShock(float stopDuration, float slowReduction, float slowDuration)
    {
        controlEffects.Apply(stopDuration, slowReduction, slowDuration);
    }

    private void OnDisable() => controlEffects.Clear();

    // 분열로 태어난 몬스터는 경로 처음이 아니라 부모 위치에서 이어 간다.
    private bool continueFromCurrent = false;

    public int CurrentWaypointIndex => currentWaypointIndex;

    // 얼음 길 등 바깥에서 잠깐 주는 가속
    private float boostMultiplier = 1f;
    private float boostUntil;

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (Time.time >= boostUntil || multiplier >= boostMultiplier)
            boostMultiplier = multiplier;
        boostUntil = Mathf.Max(boostUntil, Time.time + duration);
    }

    private float CurrentBoost => Time.time < boostUntil ? boostMultiplier : 1f;

    /// <summary>경로를 따라 distance만큼 앞으로 순간 이동합니다. 기지를 넘어가지는 않습니다.</summary>
    public void AdvanceAlongPath(float distance)
    {
        if (waypoints == null)
            return;

        // 마지막 웨이포인트(기지 앞)는 건너뛰지 않는다.
        while (distance > 0f && currentWaypointIndex < waypoints.Length - 1)
        {
            Vector3 target = waypoints[currentWaypointIndex].position;
            float d = Vector3.Distance(transform.position, target);

            if (d > distance)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, distance);
                return;
            }

            transform.position = target;
            distance -= d;
            currentWaypointIndex++;
        }
    }

    public void ContinueFrom(Transform[] path, int waypointIndex)
    {
        waypoints = path;
        currentWaypointIndex = waypointIndex;
        continueFromCurrent = true;
    }

    void Awake()
    {
        ApplyStatsFromData();

        if (monsterData != null)
        {
            moveSpeed = monsterData.moveSpeed;
        }
    }

    void Start()
    {
        if (continueFromCurrent)
            return;

        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = 1;
        }
    }

    void Update()
    {
        // 빙결 중이면 이동 정지
        if (isFrozen || controlEffects.IsStunned) return;

        if (waypoints == null || waypoints.Length == 0) return;

        if (currentWaypointIndex < waypoints.Length)
        {
            Transform target = waypoints[currentWaypointIndex];

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                EffectiveMoveSpeed * CurrentBoost * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, target.position) < 0.01f)
            {
                transform.position = target.position;
                currentWaypointIndex++;
            }
        }
        else
        {
            ReachShield();
        }
    }

    public void ApplyStatsFromData()
    {
        if (monsterData == null)
            return;

        baseDamage = monsterData.baseDamage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Shield"))
        {
            ReachShield();
        }
    }

    private void ReachShield()
    {
        if (hasReachedShield)
            return;

        hasReachedShield = true;

        // 연습 몬스터가 도착해도 실제 기지 체력과 웨이브 상태는 그대로 둔다.
        if (GetComponent<TutorialPracticeMonster>() == null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TakePlayerDamage(baseDamage, transform.position);

            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
                waveManager.OnMonsterPassed();
        }

        Destroy(gameObject);
    }

    // 마법화살 루트용 슬로우 기능
    public void ApplySlow(float slowRate, float duration)
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            moveSpeed = monsterData.moveSpeed;
        }

        slowCoroutine = StartCoroutine(SlowRoutine(slowRate, duration));
    }

    IEnumerator SlowRoutine(float slowRate, float duration)
    {
        moveSpeed = monsterData.moveSpeed * slowRate;

        yield return new WaitForSeconds(duration);

        moveSpeed = monsterData.moveSpeed;
        slowCoroutine = null;

    }

    // 스킬용 빙결 기능
    public bool IsFrozen => isFrozen;

    public void Freeze(float duration)
    {
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }

        freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;

        yield return new WaitForSeconds(duration);

        isFrozen = false;
        freezeCoroutine = null;

    }

    public void Die()
    {
        if (GetComponent<TutorialPracticeMonster>() == null)
        {
            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
                waveManager.OnMonsterKilled();
        }

        Destroy(gameObject);
    }

    void OnAnimationEvent() { }
}
