using UnityEngine;

public class MonsterMove : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 1.5f;
    private int currentWaypointIndex = 0;

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

            // 몬스터가 끝까지 도착하면 플레이어 체력 감소
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakePlayerDamage(1);
            }

            // 웨이브 시스템에 몬스터가 통과했다고 알림
            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.OnMonsterPassed();
            }

            Destroy(gameObject);
        }
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
