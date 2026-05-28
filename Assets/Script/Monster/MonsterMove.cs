using System.Collections;
using UnityEngine;

public class MonsterMove : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 1.5f;

    private float originalMoveSpeed;
    private Coroutine slowCoroutine;

    private int currentWaypointIndex = 0;

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





