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

    private int runningCoroutinesCount = 0;

    public bool HasSpawnError { get; protected set; }

    public virtual IEnumerator SpawnWave(
        WaveData wave,
        Action<GameObject> onSpawned)
    {
        HasSpawnError = false;
        runningCoroutinesCount = 0;

        if (wave == null)
        {
            HasSpawnError = true;

            Debug.LogError(
                "MonsterSpawner: WaveData가 없습니다.",
                this
            );

            yield break;
        }

        if (wave.spawnInfos == null ||
            wave.spawnInfos.Length == 0)
        {
            HasSpawnError = true;

            Debug.LogError(
                "MonsterSpawner: SpawnInfo가 없습니다.",
                this
            );

            yield break;
        }

        /*
         * Stage 4 경로가 일부만 연결된 경우 오류
         */
        if (HasAnyStage4Routes() &&
            !HasStage4Routes())
        {
            HasSpawnError = true;

            Debug.LogError(
                "MonsterSpawner: Stage 4의 4개 경로가 모두 연결되어야 합니다.",
                this
            );

            yield break;
        }

        /*
         * 생성 전에 모든 SpawnInfo를 검사한다.
         */
        foreach (WaveData.SpawnInfo info in wave.spawnInfos)
        {
            if (!ValidateSpawnInfo(info))
            {
                HasSpawnError = true;
                yield break;
            }

            Transform[] route =
                GetPathWaypoints(info.pathIndex);

            if (route == null ||
                route.Length == 0)
            {
                HasSpawnError = true;

                Debug.LogError(
                    $"MonsterSpawner: Path{info.pathIndex}의 웨이포인트가 연결되지 않았습니다.",
                    this
                );

                yield break;
            }
        }

        /*
         * Stage 4
         *
         * WaveData의 pathIndex를 사용한다.
         */
        if (HasStage4Routes())
        {
            foreach (WaveData.SpawnInfo info in wave.spawnInfos)
            {
                Transform[] route =
                    GetPathWaypoints(info.pathIndex);

                for (int i = 0;
                     i < info.count;
                     i++)
                {
                    GameObject monster =
                        SpawnOneMonster(
                            info.monsterPrefab,
                            route
                        );

                    if (monster == null)
                    {
                        HasSpawnError = true;
                        yield break;
                    }

                    onSpawned?.Invoke(monster);

                    yield return new WaitForSeconds(
                        info.interval
                    );
                }
            }

            yield break;
        }

        /*
         * Stage 1 / Stage 3
         *
         * SpawnInfo별로 동시에 생성한다.
         * Stage 3은 pathIndex에 따라
         * 기본/보조 경로를 선택한다.
         */
        foreach (WaveData.SpawnInfo info in wave.spawnInfos)
        {
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

        Transform[] route =
            GetPathWaypoints(info.pathIndex);

        if (route == null ||
            route.Length == 0)
        {
            HasSpawnError = true;

            Debug.LogError(
                $"MonsterSpawner: Path{info.pathIndex}의 웨이포인트가 없습니다.",
                this
            );

            runningCoroutinesCount--;

            yield break;
        }

        for (int i = 0;
             i < info.count;
             i++)
        {
            GameObject monster =
                SpawnOneMonster(
                    info.monsterPrefab,
                    route
                );

            if (monster == null)
            {
                HasSpawnError = true;

                runningCoroutinesCount--;

                yield break;
            }

            onSpawned?.Invoke(monster);

            yield return new WaitForSeconds(
                info.interval
            );
        }

        runningCoroutinesCount--;
    }

    private GameObject SpawnOneMonster(
        GameObject prefab,
        Transform[] selectedWaypoints)
    {
        if (prefab == null)
        {
            Debug.LogError(
                "MonsterSpawner: 생성할 Prefab이 없습니다.",
                this
            );

            return null;
        }

        if (selectedWaypoints == null ||
            selectedWaypoints.Length == 0)
        {
            Debug.LogError(
                "MonsterSpawner: 사용할 웨이포인트가 없습니다.",
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

            Destroy(monster);

            return null;
        }

        monsterMove.waypoints =
            selectedWaypoints;

        return monster;
    }

    private bool ValidateSpawnInfo(
        WaveData.SpawnInfo info)
    {
        if (info == null)
        {
            Debug.LogError(
                "MonsterSpawner: SpawnInfo가 비어 있습니다.",
                this
            );

            return false;
        }

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

        if (HasStage4Routes())
        {
            if (info.pathIndex < 1 ||
                info.pathIndex > 4)
            {
                Debug.LogError(
                    $"MonsterSpawner: Stage 4에서는 Path Index 1~4만 사용할 수 있습니다. 입력값={info.pathIndex}",
                    this
                );

                return false;
            }
        }
        else if (useMultipleRoutes)
        {
            if (info.pathIndex < 1 ||
                info.pathIndex > 2)
            {
                Debug.LogError(
                    $"MonsterSpawner: Stage 3에서는 Path Index 1~2만 사용할 수 있습니다. 입력값={info.pathIndex}",
                    this
                );

                return false;
            }
        }
        else
        {
            if (info.pathIndex != 1)
            {
                Debug.LogError(
                    $"MonsterSpawner: 현재 스테이지에서는 Path Index 1만 사용할 수 있습니다. 입력값={info.pathIndex}",
                    this
                );

                return false;
            }
        }

        return true;
    }

    private Transform[] GetPathWaypoints(
        int pathIndex)
    {
        /*
         * Stage 4
         */
        if (HasStage4Routes())
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
                        $"MonsterSpawner: Stage 4의 잘못된 Path Index({pathIndex})입니다.",
                        this
                    );

                    return null;
            }
        }

        /*
         * Stage 3
         */
        if (useMultipleRoutes)
        {
            switch (pathIndex)
            {
                case 1:
                    return waypoints;

                case 2:
                    return secondaryWaypoints;

                default:
                    Debug.LogError(
                        $"MonsterSpawner: Stage 3에서는 Path Index 1~2만 사용할 수 있습니다. 입력값={pathIndex}",
                        this
                    );

                    return null;
            }
        }

        /*
         * Stage 1
         */
        if (pathIndex != 1)
        {
            Debug.LogError(
                $"MonsterSpawner: 현재 스테이지에서는 Path 1만 사용할 수 있습니다. 입력값={pathIndex}",
                this
            );

            return null;
        }

        return waypoints;
    }

    private bool HasStage4Routes()
    {
        return HasWaypoints(path1Waypoints) &&
               HasWaypoints(path2Waypoints) &&
               HasWaypoints(path3Waypoints) &&
               HasWaypoints(path4Waypoints);
    }

    private bool HasAnyStage4Routes()
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
}



