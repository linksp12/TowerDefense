using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "TowerDefense/WaveData")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public class SpawnInfo
    {
        [Header("몬스터")]
        public GameObject monsterPrefab;

        [Header("스폰 설정")]

        [Min(1)]
        public int count = 1;

        [Min(0.01f)]
        public float interval = 1f;

        [Header("출발 길")]
        [Tooltip("1 = Path1, 2 = Path2, 3 = Path3, 4 = Path4")]
        [Range(1, 4)]
        public int pathIndex = 1;
    }

    [Header("이번 웨이브 몬스터 목록")]
    public SpawnInfo[] spawnInfos;

    [Header("웨이브 시작 전 대기")]
    [Min(0f)]
    public float waveStartDelay = 4f;

    private void OnValidate()
    {
        if (spawnInfos == null || spawnInfos.Length == 0)
        {
            Debug.LogError(
                $"{name}: SpawnInfo가 없습니다.",
                this
            );
            return;
        }

        for (int i = 0; i < spawnInfos.Length; i++)
        {
            SpawnInfo info = spawnInfos[i];

            if (info == null)
            {
                Debug.LogError(
                    $"{name}: SpawnInfo[{i}]가 비어 있습니다.",
                    this
                );
                continue;
            }

            if (info.monsterPrefab == null)
            {
                Debug.LogError(
                    $"{name}: SpawnInfo[{i}]의 Monster Prefab이 없습니다.",
                    this
                );
            }

            if (info.count < 1)
            {
                Debug.LogError(
                    $"{name}: SpawnInfo[{i}]의 Count는 1 이상이어야 합니다.",
                    this
                );
            }

            if (info.interval <= 0f)
            {
                Debug.LogError(
                    $"{name}: SpawnInfo[{i}]의 Interval은 0보다 커야 합니다.",
                    this
                );
            }

            if (info.pathIndex < 1 || info.pathIndex > 4)
            {
                Debug.LogError(
                    $"{name}: SpawnInfo[{i}]의 Path Index가 잘못되었습니다.",
                    this
                );
            }
        }

        if (waveStartDelay < 0f)
        {
            Debug.LogError(
                $"{name}: Wave Start Delay는 0 이상이어야 합니다.",
                this
            );
        }
    }
}
