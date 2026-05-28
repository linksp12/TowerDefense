using UnityEngine;
using System.Collections;

public class MonsterMove : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 1.5f;
    private int currentWaypointIndex = 0;

    // ✅ 추가
    private bool isFrozen = false;

    void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = 1;
        }
    }

    void Update()
    {
        // ✅ 추가 - 빙결 중이면 이동 정지
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
                GameManager.Instance.TakePlayerDamage(1);

            WaveManager waveManager = FindAnyObjectByType<WaveManager>();
            if (waveManager != null)
                waveManager.OnMonsterPassed();

            Destroy(gameObject);
        }
    }

    // ✅ 추가 - 빙결 함수
    public void Freeze(float duration)
    {
        StartCoroutine(FreezeCoroutine(duration));
    }

    private IEnumerator FreezeCoroutine(float duration)
    {
        isFrozen = true;
        yield return new WaitForSeconds(duration);
        isFrozen = false;
    }

    public void Die()
    {
        WaveManager waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager != null)
            waveManager.OnMonsterKilled();

        Destroy(gameObject);
    }

    void OnAnimationEvent() { }
}