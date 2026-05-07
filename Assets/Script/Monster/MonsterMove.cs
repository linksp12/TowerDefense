using UnityEngine;

public class MonsterMove : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 2f;

    private int currentWaypointIndex = 0;

    void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position; // 0번에서 시작
            currentWaypointIndex = 1; // 다음 목표는 1번
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        if (currentWaypointIndex < waypoints.Length)
        {
            Transform target = waypoints[currentWaypointIndex];

            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, target.position) < 0.05f)
            {
                currentWaypointIndex++;
            }
        }
        else
        {
            Debug.Log("몬스터 도착");
            Destroy(gameObject);
        }
    }
}
