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
        public int count = 1;
        public float interval = 1f;

        [Header("출발 길")]
        [Tooltip("1 = Path1, 2 = Path2, 3 = Path3, 4 = Path4")]
        [Range(1, 4)]
        public int pathIndex = 1;
    }

    [Header("이번 웨이브 몬스터 목록")]
    public SpawnInfo[] spawnInfos;

    [Header("웨이브 시작 전 대기")]
    public float waveStartDelay = 4f;
}
