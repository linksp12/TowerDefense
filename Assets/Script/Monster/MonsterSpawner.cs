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

    private int nextRouteIndex = 0;
    private int runningCoroutinesCount = 0;

    public virtual IEnumerator SpawnWave(
        WaveData wave,
        Action<GameObject> onSpawned)
    {
        if (wave == null)
        {
            Debug.LogError(
                "MonsterSpawner: WaveData가 없습니다.",
                this
            );

            yield break;
        }

        if (wave.spawnInfos == null ||
            wave.spawnInfos.Length == 0)
        {
            Debug.LogError(
                "MonsterSpawner: SpawnInfo가 없습니다.",
                this
            );

            yield break;
        }

        nextRouteIndex = 0;

        /*
         * Stage 4
         *
         * 4개 경로가 연결된 경우
         * WaveData의 pathIndex를 사용한다.
         */
        if (HasStage4Routes())
        {
            foreach (WaveData.SpawnInfo info in wave.spawnInfos)
            {
                if (info == null)
                {
                    Debug.LogError(
                        "MonsterSpawner: SpawnInfo가 비어 있습니다.",
                        this
                    );

                    continue;
                }

                if (!IsSpawnInfoValid(info))
                    continue;

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

                    yield return new WaitForSeconds(
                        info.interval
                    );
                }
            }

            yield break;
        }

        /*
         * Stage 1 및 Stage 3
         *
         * 기존 구조 유지
         */
        foreach (WaveData.SpawnInfo info in wave.spawnInfos)
        {
            if (info == null)
            {
                Debug.LogError(
                    "MonsterSpawner: SpawnInfo가 비어 있습니다.",
                    this
                );

                continue;
            }

            if (!IsSpawnInfoValid(info))
                continue;

            StartCoroutine(
                SpawnSingleInfo(
                    info,
                    onSpawned
                )
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

        if (!IsSpawnInfoValid(info))
        {
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

            yield return new WaitForSeconds(
                info.interval
            );
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
                "MonsterSpawner: 생성할 Prefab이 없습니다.",
                this
            );

            return null;
        }

        Transform[] selectedWaypoints;

        /*
         * Stage 4
         */
        if (HasStage4Routes())
        {
            selectedWaypoints =
                GetPathWaypoints(pathIndex);
        }
        /*
         * Stage 3
         * 기존 2개 경로 번갈아 생성 유지
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
         * Stage 1
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
                $"MonsterSpawner: Path{pathIndex}의 웨이포인트가 연결되지 않았습니다.",
                this
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
                $"MonsterSpawner: {prefab.name}에 MonsterMove가 없습니다.",
                this
            );

            return monster;
        }

        monsterMove.waypoints = selectedWaypoints;

        return monster;
    }

    private bool IsSpawnInfoValid(
        WaveData.SpawnInfo info)
    {
        if (info == null)
            return false;

        if (info.monsterPrefab == null)
        {
            Debug.LogError(
                "MonsterSpawner: Monster Prefab이 없습니다.",
                this
            );

            return false;
        }

        if (info.count < 1)
        {
            Debug.LogError(
                $"MonsterSpawner: Count는 1 이상이어야 합니다. count={info.count}",
                this
            );

            return false;
        }

        if (info.interval <= 0f)
        {
            Debug.LogError(
                $"MonsterSpawner: Interval은 0보다 커야 합니다. interval={info.interval}",
                this
            );

            return false;
        }

        if (info.pathIndex < 1 ||
            info.pathIndex > 4)
        {
            Debug.LogError(
                $"MonsterSpawner: 잘못된 pathIndex({info.pathIndex})입니다.",
                this
            );

            return false;
        }

        return true;
    }

    private bool HasStage4Routes()
    {
        return HasWaypoints(path1Waypoints) ||
               HasWaypoints(path2Waypoints) ||
               HasWaypoints(path3Waypoints) ||
               HasWaypoints(path4Waypoints);
    }

    private bool HasWaypoints(
        Transform[] targetWaypoints)
    {
        return targetWaypoints != null &&
               targetWaypoints.Length > 0;
    }

    private Transform[] GetPathWaypoints(
        int pathIndex)
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
                Debug.LogError(
                    $"MonsterSpawner: 잘못된 pathIndex({pathIndex})입니다.",
                    this
                );

                return null;
        }
    }
}



