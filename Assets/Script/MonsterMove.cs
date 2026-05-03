using UnityEngine;

public class MonsterMove : MonoBehaviour
{
    // 사용법: 씬에 Waypoint들을 배치하고, 
    // 본 스크립트의 Waypoints 배열에 순서대로 연결해서 사용하세요.
    public Transform[] waypoints; //목표 지점
    public float moveSpeed = 2f; //이동 속도
    private int currentWaypointIndex = 0;

    void Update()
    {
        if (currentWaypointIndex < waypoints.Length)
        {
            transform.position = Vector2.MoveTowards(transform.position, 
                waypoints[currentWaypointIndex].position, moveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, waypoints[currentWaypointIndex].position) < 0.1f)
            {
                currentWaypointIndex++;
            }
        }
    }
}
