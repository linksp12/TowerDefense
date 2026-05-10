using UnityEngine;

public class MonsterMove : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 1.5f;        // 속도 줄임
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

            // Vector3으로 변경 → 더 부드럽게
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            // 거리 체크 정밀하게 + 위치 정확히 맞추기
            if (Vector3.Distance(transform.position, target.position) < 0.01f)
            {
                transform.position = target.position;
                currentWaypointIndex++;
            }
        }
        else
        {
            FindObjectOfType<WaveManager>()?.OnMonsterPassed();
            Destroy(gameObject);
        }
    }

    public void Die()
    {
        FindObjectOfType<WaveManager>()?.OnMonsterKilled();
        Destroy(gameObject);
    }

    void OnAnimationEvent() { }
}
