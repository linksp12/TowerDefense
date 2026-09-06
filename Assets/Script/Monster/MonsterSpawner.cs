using System;
using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("기본 경로")]
    public GameObject monsterPrefab;
    public Transform[] waypoints;

    [Header("2개 경로 설정 (Stage 3)")]
    public Transform[] secondaryWaypoints;
    public bool useMultipleRoutes = false;

    [Header("4개 출발 경로 (Stage 4)")]
    public Transform[] path1Waypoints;
    public Transform[] path2Waypoints;
    public Transform[] path3Waypoints;
    public Transform[] path4Waypoints;

    [Header("기본 스폰 설정")]
    public int spawnCount = 5;
    public float spawnInterval = 1f;

    private int nextRouteIndex = 0;
    private int runningCoroutinesCount = 0;

    public virtual IEnumerator SpawnWave(
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

        nextRouteIndex = 0;

        /*
         * Stage 4
         *
         * path1~4 중 하나라도 연결되어 있으면
         * WaveData의 pathIndex를 사용하는 4경로 방식으로 작동한다.
         */
        if (HasStage4Routes())
        {
            foreach (WaveData.SpawnInfo info in wave.spawnInfos)
            {
                if (info == null)
                {
                    continue;
                }

                if (info.monsterPrefab == null)
                {
                    Debug.LogError(
                        "MonsterSpawner: Monster Prefab이 없습니다."
                    );

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

            yield break;
        }

        /*
         * Stage 1 및 Stage 3
         *
         * 각 SpawnInfo를 동시에 실행한다.
         * Stage 3에서는 useMultipleRoutes가 켜져 있으면
         * 기본 경로와 두 번째 경로를 번갈아 사용한다.
         */
        foreach (WaveData.SpawnInfo info in wave.spawnInfos)
        {
            if (info == null)
            {
                continue;
            }

            StartCoroutine(
                SpawnSingleInfo(info, onSpawned)
            );
        }

        yield return new WaitUntil(
            () => runningCoroutinesCount <= 0
        );
    }

    private IEnumerator SpawnSingleInfo(
        WaveData.SpawnInfo info,
        Action<GameObject> onSpawned)
    {
        runningCoroutinesCount++;

        if (info.monsterPrefab == null)
        {
            Debug.LogError(
                "MonsterSpawner: Monster Prefab이 없습니다."
            );

            runningCoroutinesCount--;
            yield break;
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

        runningCoroutinesCount--;
    }

    private GameObject SpawnOneMonster(
        GameObject prefab,
        int pathIndex)
    {
        if (prefab == null)
        {
            Debug.LogError(
                "MonsterSpawner: 생성할 Prefab이 없습니다."
            );

            return null;
        }

        Transform[] selectedWaypoints;

        /*
         * Stage 4의 4개 경로
         */
        if (HasStage4Routes())
        {
            selectedWaypoints = GetPathWaypoints(pathIndex);
        }
        /*
         * Stage 3의 2개 경로
         */
        else if (
            useMultipleRoutes &&
            secondaryWaypoints != null &&
            secondaryWaypoints.Length > 0)
        {
            if (nextRouteIndex % 2 == 0)
            {
                selectedWaypoints = waypoints;
            }
            else
            {
                selectedWaypoints = secondaryWaypoints;
            }

            nextRouteIndex++;
        }
        /*
         * Stage 1 및 일반 스테이지 기본 경로
         */
        else
        {
            selectedWaypoints = waypoints;
        }

        if (
            selectedWaypoints == null ||
            selectedWaypoints.Length == 0)
        {
            Debug.LogError(
                $"MonsterSpawner: Path{pathIndex}의 웨이포인트가 연결되지 않았습니다."
            );

            return null;
        }

        GameObject monster = Instantiate(
            prefab,
            selectedWaypoints[0].position,
            Quaternion.identity
        );

        MonsterMove monsterMove =
            monster.GetComponent<MonsterMove>();

        if (monsterMove == null)
        {
            Debug.LogError(
                $"MonsterSpawner: {prefab.name}에 MonsterMove가 없습니다."
            );

            return monster;
        }

        monsterMove.waypoints = selectedWaypoints;

        return monster;
    }

    private bool HasStage4Routes()
    {
        return HasWaypoints(path1Waypoints) ||
               HasWaypoints(path2Waypoints) ||
               HasWaypoints(path3Waypoints) ||
               HasWaypoints(path4Waypoints);
    }

    private bool HasWaypoints(Transform[] targetWaypoints)
    {
        return targetWaypoints != null &&
               targetWaypoints.Length > 0;
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
                    $"MonsterSpawner: 잘못된 pathIndex({pathIndex})입니다. Path1을 사용합니다."
                );

                return path1Waypoints;
        }
    }
}



