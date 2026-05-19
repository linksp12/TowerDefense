using System;
using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;
    public Transform[] waypoints;
    public int spawnCount = 5;
    public float spawnInterval = 1f;

    // ↑ Start() 와 SpawnMonsters() 삭제함
    // WaveManager가 대신 스폰을 제어하기 때문

    public IEnumerator SpawnWave(WaveData wave, Action<GameObject> onSpawned)
    {
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
            monsterMove.waypoints = waypoints;
        }
        return monster;
    }
}
