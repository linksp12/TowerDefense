using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 특수 능력에서 함께 쓰는 기능: 경로 위 소환, 주변 타워 찾기, 화면 흔들림.
/// </summary>
public static class MonsterAbilityUtil
{
    /// <summary>
    /// from 몬스터가 걷던 경로를 이어서 걷는 몬스터를 count마리 만들고 웨이브 남은 수에 더합니다.
    /// </summary>
    public static List<GameObject> SpawnOnPath(GameObject prefab, MonsterMove from, Vector3 origin, int count, float spread)
    {
        List<GameObject> spawned = new List<GameObject>();

        if (prefab == null || from == null || from.waypoints == null || from.waypoints.Length == 0)
            return spawned;

        int index = Mathf.Clamp(from.CurrentWaypointIndex, 0, from.waypoints.Length);
        WaveManager waveManager = Object.FindAnyObjectByType<WaveManager>();

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : (i / (float)(count - 1)) * 2f - 1f;
            Vector3 offset = new Vector3(t * spread, Random.Range(-spread, spread) * 0.4f, 0f);
            GameObject monster = Object.Instantiate(prefab, origin + offset, Quaternion.identity);

            MonsterMove move = monster.GetComponent<MonsterMove>();
            if (move == null)
            {
                Debug.LogError($"MonsterAbilityUtil: {prefab.name}에 MonsterMove가 없습니다.");
                Object.Destroy(monster);
                continue;
            }

            move.ContinueFrom(from.waypoints, index);

            if (waveManager != null)
                waveManager.RegisterSpawnedMonster(monster);

            spawned.Add(monster);
        }

        return spawned;
    }

    /// <summary>center에서 range 안의 타워를 가까운 순으로 최대 maxCount개 돌려줍니다.</summary>
    public static List<TowerAttack> FindNearbyTowers(Vector3 center, float range, int maxCount)
    {
        List<TowerAttack> result = new List<TowerAttack>();
        TowerAttack[] towers = Object.FindObjectsByType<TowerAttack>(FindObjectsInactive.Exclude);

        foreach (TowerAttack tower in towers)
        {
            if (tower != null && Vector3.Distance(center, tower.transform.position) <= range)
                result.Add(tower);
        }

        result.Sort((a, b) =>
            Vector3.Distance(center, a.transform.position).CompareTo(Vector3.Distance(center, b.transform.position)));

        if (result.Count > maxCount)
            result.RemoveRange(maxCount, result.Count - maxCount);

        return result;
    }

    public static void Shake(float amount)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        if (!camera.TryGetComponent(out CameraShake shake))
            shake = camera.gameObject.AddComponent<CameraShake>();

        shake.Add(amount, 0.5f);
    }
}
