using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Stage 2 전용 스포너입니다. 세 입구의 적을 번갈아 생성합니다.
/// 세 경로는 중앙에서 만나 대포의 광역 공격 구간을 형성합니다.
/// </summary>
public class Stage2MonsterSpawner : MonsterSpawner
{
    [Header("Additional Stage 2 Routes")]
    public Transform[] secondRoute;
    public Transform[] thirdRoute;

    public override IEnumerator SpawnWave(WaveData wave, Action<GameObject> onSpawned)
    {
        int spawnIndex = 0;

        foreach (WaveData.SpawnInfo info in wave.spawnInfos)
        {
            for (int i = 0; i < info.count; i++)
            {
                GameObject monster = Instantiate(info.monsterPrefab);
                MonsterMove monsterMove = monster.GetComponent<MonsterMove>();

                if (monsterMove != null)
                {
                    int routeIndex = spawnIndex % 3;
                    monsterMove.waypoints = routeIndex switch
                    {
                        1 when secondRoute != null && secondRoute.Length > 1 => secondRoute,
                        2 when thirdRoute != null && thirdRoute.Length > 1 => thirdRoute,
                        _ => waypoints
                    };
                }

                onSpawned?.Invoke(monster);
                spawnIndex++;
                yield return new WaitForSeconds(info.interval);
            }
        }
    }
}
