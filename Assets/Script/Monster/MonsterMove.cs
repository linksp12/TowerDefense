using System.Collections;
using UnityEngine;

public class MonsterMove : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 1.5f;

    private float originalMoveSpeed;
    private Coroutine slowCoroutine;
    private Coroutine freezeCoroutine;

    private int currentWaypointIndex = 0;

    // 스킬용 빙결 상태
    private bool isFrozen = false;

    void Start()
    {
        originalMoveSpeed = moveSpeed;

        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = 1;
        }
    }

    void Update()
    {
        // 빙결 중이면 이동 정지
        if (isFrozen) return;

        if (waypoints == null || waypoints.Length == 0) return;

        if (currentWaypointIndex < waypoints.Length)
        {
            Transform target = waypoints[currentWaypointIndex];

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, target.position) < 0.01f)
            {
                transform.position = target.position;
                currentWaypointIndex++;
            }
        }
        else
        {
            Debug.Log("몬스터 도착");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakePlayerDamage(1);
            }

            WaveManager waveManager = FindFirstObjectByType<WaveManager>();

            if (waveManager != null)
            {
                waveManager.OnMonsterPassed();
            }

            Destroy(gameObject);
        }
    }

    // 마법화살 루트용 슬로우 기능
    public void ApplySlow(float slowRate, float duration)
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            moveSpeed = originalMoveSpeed;
        }

        slowCoroutine = StartCoroutine(SlowRoutine(slowRate, duration));
    }

    IEnumerator SlowRoutine(float slowRate, float duration)
    {
        moveSpeed = originalMoveSpeed * slowRate;

        Debug.Log("슬로우 적용 / 현재 속도: " + moveSpeed);

        yield return new WaitForSeconds(duration);

        moveSpeed = originalMoveSpeed;
        slowCoroutine = null;

        Debug.Log("슬로우 해제 / 현재 속도: " + moveSpeed);
    }

    // 스킬용 빙결 기능
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

        Debug.Log("몬스터 빙결");

        yield return new WaitForSeconds(duration);

        isFrozen = false;
        freezeCoroutine = null;

        Debug.Log("몬스터 빙결 해제");
    }

    public void Die()
    {
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager != null)
        {
            waveManager.OnMonsterKilled();
        }

        Destroy(gameObject);
    }

    void OnAnimationEvent() { }
}