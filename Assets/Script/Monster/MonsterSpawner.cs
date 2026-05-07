using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;     // 생성할 몬스터 프리팹
    public Transform[] waypoints;        // 몬스터가 따라갈 경로
    public int spawnCount = 5;           // 생성할 몬스터 수
    public float spawnInterval = 1f;     // 생성 간격

    void Start()
    {
        StartCoroutine(SpawnMonsters());
    }

    IEnumerator SpawnMonsters()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject monster = Instantiate(monsterPrefab);

            MonsterMove monsterMove = monster.GetComponent<MonsterMove>();

            if (monsterMove != null)
            {
                monsterMove.waypoints = waypoints;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
