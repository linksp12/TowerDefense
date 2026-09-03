using System;
using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;
    public Transform[] waypoints;

    [Header("다중 경로 설정")]
    public Transform[] secondaryWaypoints;
    public bool useMultipleRoutes = false;

    private int nextRouteIndex = 0;
    
    public int spawnCount = 5;
    public float spawnInterval = 1f;

    // ↑ Start() 와 SpawnMonsters() 삭제함
    // WaveManager가 대신 스폰을 제어하기 때문

    public IEnumerator SpawnWave(WaveData wave, Action<GameObject> onSpawned)
    {
        nextRouteIndex = 0;

        foreach (var info in wave.spawnInfos)
        {
            for (int i = 0; i < info.count; i++)
            {
                GameObject monster = SpawnOneMonster(info.monsterPrefab);
                onSpawned?.Invoke(monster);
                yield return new WaitForSeconds(info.interval);
            }
        }
    }

    GameObject SpawnOneMonster(GameObject prefab)
    {
        GameObject monster = Instantiate(prefab);

        MonsterMove monsterMove = monster.GetComponent<MonsterMove>();

        if (monsterMove != null)
        {
            bool canUseSecondRoute =
                useMultipleRoutes &&
                secondaryWaypoints != null &&
                secondaryWaypoints.Length > 0;

            if (canUseSecondRoute)
            {
                if (nextRouteIndex % 2 == 0)
                {
                    monsterMove.waypoints = waypoints;
                }
                else
                {
                    monsterMove.waypoints = secondaryWaypoints;
                }

                nextRouteIndex++;
            }
            else
            {
                monsterMove.waypoints = waypoints;
            }
        }

        return monster;
    }
}



