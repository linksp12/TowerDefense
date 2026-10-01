using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Stage 2 전용 스포너입니다.
/// WaveData의 pathIndex에 따라 3개 경로 중 하나를 선택합니다.
/// </summary>
public class Stage2MonsterSpawner : MonsterSpawner
{
    [Header("Additional Stage 2 Routes")]
    public Transform[] secondRoute;
    public Transform[] thirdRoute;

    public override IEnumerator SpawnWave(
        WaveData wave,
        Action<GameObject> onSpawned)
    {
        if (wave == null)
        {
            Debug.LogError(
                "Stage2MonsterSpawner: WaveData가 없습니다.",
                this
            );

            yield break;
        }

        if (wave.spawnInfos == null ||
            wave.spawnInfos.Length == 0)
        {
            Debug.LogError(
                "Stage2MonsterSpawner: SpawnInfo가 없습니다.",
                this
            );

            yield break;
        }

        if (!HasStage2Routes())
        {
            Debug.LogError(
                "Stage2MonsterSpawner: Stage 2 경로가 연결되지 않았습니다.",
                this
            );

            yield break;
        }

        foreach (WaveData.SpawnInfo info in wave.spawnInfos)
        {
            if (info == null)
            {
                Debug.LogError(
                    "Stage2MonsterSpawner: SpawnInfo가 비어 있습니다.",
                    this
                );

                yield break;
            }

            if (info.monsterPrefab == null)
            {
                Debug.LogError(
                    "Stage2MonsterSpawner: Monster Prefab이 없습니다.",
                    this
                );

                yield break;
            }

            if (info.count < 1)
            {
                Debug.LogError(
                    $"Stage2MonsterSpawner: Count는 1 이상이어야 합니다. count={info.count}",
                    this
                );

                yield break;
            }

            if (info.interval <= 0f)
            {
                Debug.LogError(
                    $"Stage2MonsterSpawner: Interval은 0보다 커야 합니다. interval={info.interval}",
                    this
                );

                yield break;
            }

            Transform[] route =
                GetStage2Route(info.pathIndex);

            if (route == null || route.Length == 0)
            {
                Debug.LogError(
                    $"Stage2MonsterSpawner: Path{info.pathIndex}가 연결되지 않았습니다.",
                    this
                );

                yield break;
            }

            for (int i = 0; i < info.count; i++)
            {
                GameObject monster = Instantiate(
                    info.monsterPrefab,
                    route[0].position,
                    Quaternion.identity
                );

                MonsterMove monsterMove =
                    monster.GetComponent<MonsterMove>();

                if (monsterMove == null)
                {
                    Debug.LogError(
                        $"Stage2MonsterSpawner: {info.monsterPrefab.name}에 MonsterMove가 없습니다.",
                        this
                    );

                    yield break;
                }

                monsterMove.waypoints = route;

                onSpawned?.Invoke(monster);

                yield return new WaitForSeconds(
                    info.interval
                );
            }
        }
    }

    private Transform[] GetStage2Route(int pathIndex)
    {
        switch (pathIndex)
        {
            case 1:
                return waypoints;

            case 2:
                return secondRoute;

            case 3:
                return thirdRoute;

            default:
                Debug.LogError(
                    $"Stage2MonsterSpawner: 잘못된 pathIndex({pathIndex})입니다. " +
                    "Stage 2에서는 1~3만 사용할 수 있습니다.",
                    this
                );

                return null;
        }
    }

    private bool HasStage2Routes()
    {
        return waypoints != null &&
               waypoints.Length > 0 &&
               secondRoute != null &&
               secondRoute.Length > 0 &&
               thirdRoute != null &&
               thirdRoute.Length > 0;
    }
}
