using System;
using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("4개 출발 경로")]
    public Transform[] path1Waypoints;
    public Transform[] path2Waypoints;
    public Transform[] path3Waypoints;
    public Transform[] path4Waypoints;

    public IEnumerator SpawnWave(
        WaveData wave,
        Action<GameObject> onSpawned)
    {
        if (wave == null)
        {
            Debug.LogError("MonsterSpawner: WaveData가 없습니다.");
            yield break;
        }

        if (wave.spawnInfos == null || wave.spawnInfos.Length == 0)
        {
            Debug.LogWarning("MonsterSpawner: SpawnInfo가 없습니다.");
            yield break;
        }

        foreach (var info in wave.spawnInfos)
        {
            if (info == null)
                continue;

            if (info.monsterPrefab == null)
            {
                Debug.LogError("MonsterSpawner: Monster Prefab이 없습니다.");
                continue;
            }

            for (int i = 0; i < info.count; i++)
            {
                GameObject monster = SpawnOneMonster(
                    info.monsterPrefab,
                    info.pathIndex
                );

                if (monster != null)
                {
                    onSpawned?.Invoke(monster);
                }

                yield return new WaitForSeconds(info.interval);
            }
        }
    }

    private GameObject SpawnOneMonster(
        GameObject prefab,
        int pathIndex)
    {
        Transform[] selectedWaypoints = GetPathWaypoints(pathIndex);

        if (selectedWaypoints == null || selectedWaypoints.Length == 0)
        {
            Debug.LogError(
                $"MonsterSpawner: Path{pathIndex} 웨이포인트가 연결되지 않았습니다."
            );

            return null;
        }

        // 첫 번째 웨이포인트 위치에서 생성
        GameObject monster = Instantiate(
            prefab,
            selectedWaypoints[0].position,
            Quaternion.identity
        );

        MonsterMove monsterMove = monster.GetComponent<MonsterMove>();

        if (monsterMove == null)
        {
            Debug.LogError(
                $"MonsterSpawner: {prefab.name}에 MonsterMove가 없습니다."
            );

            return monster;
        }

        // 선택된 길의 웨이포인트를 몬스터에게 전달
        monsterMove.waypoints = selectedWaypoints;

        return monster;
    }

    private Transform[] GetPathWaypoints(int pathIndex)
    {
        switch (pathIndex)
        {
            case 1:
                return path1Waypoints;

            case 2:
                return path2Waypoints;

            case 3:
                return path3Waypoints;

            case 4:
                return path4Waypoints;

            default:
                Debug.LogWarning(
                    $"MonsterSpawner: 잘못된 pathIndex ({pathIndex})입니다. Path1을 사용합니다."
                );

                return path1Waypoints;
        }
    }
}



