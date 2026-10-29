using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("기본 경로")]
    public GameObject monsterPrefab;
    public Transform[] waypoints;

    [Header("2개 경로 설정 (Stage 3)")]
    public Transform[] secondaryWaypoints;
    public bool useMultipleRoutes = false;

    [Header("여러 출발 경로 (Stage 4 이후, Path1부터 필요한 개수만)")]
    public Transform[] path1Waypoints;
    public Transform[] path2Waypoints;
    public Transform[] path3Waypoints;
    public Transform[] path4Waypoints;

    private int runningCoroutinesCount = 0;

    // Inspector 배열에 남은 빈칸(삭제된 웨이포인트)을 뺀 경로. 원본 배열 단위로 한 번만 만든다.
    private readonly Dictionary<Transform[], Transform[]> compactRoutes =
        new Dictionary<Transform[], Transform[]>();

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
         * 여러 경로 모드는 Path1부터 필요한 개수만 연결한다.
         * 웨이브가 쓰는 경로가 비어 있으면 아래 검사에서 막는다.
         */

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
                    $"MonsterSpawner: 여러 경로 모드에서는 Path Index 1~4만 사용할 수 있습니다. 입력값={info.pathIndex}",
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
        Transform[] route = GetRawPathWaypoints(pathIndex);

        if (route == null)
            return null;

        if (!compactRoutes.TryGetValue(route, out Transform[] compact))
        {
            compact = Array.FindAll(route, waypoint => waypoint != null);
            compactRoutes[route] = compact;
        }

        return compact;
    }

    private Transform[] GetRawPathWaypoints(
        int pathIndex)
    {
        /*
         * Stage 4 이후 여러 경로
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
                        $"MonsterSpawner: 여러 경로 모드의 잘못된 Path Index({pathIndex})입니다.",
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

    // Path1이 연결돼 있으면 여러 경로 모드다. Path2~4는 맵에 필요한 만큼만 연결한다.
    private bool HasStage4Routes()
    {
        return HasWaypoints(path1Waypoints);
    }

    private bool HasWaypoints(
        Transform[] targetWaypoints)
    {
        return targetWaypoints != null &&
               Array.Exists(targetWaypoints, waypoint => waypoint != null);
    }
}



