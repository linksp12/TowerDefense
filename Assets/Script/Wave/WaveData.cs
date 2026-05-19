using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "TowerDefense/WaveData")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public class SpawnInfo
    {
        public GameObject monsterPrefab;  // 스폰할 몬스터 프리팹
        public int count;                 // 스폰 수
        public float interval;            // 스폰 간격 (초)
    }

    public SpawnInfo[] spawnInfos;        // 이번 웨이브의 몬스터 목록
    public float waveStartDelay = 3f;     // 웨이브 시작 전 대기 시간
}
