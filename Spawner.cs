using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public GameObject enemyPrefab; // 소환할 적 프리팹
    public float timeBetweenWaves = 3f; // 소환 간격
    private float countdown = 2f; // 첫 소환까지 남은 시간

    void Update()
    {
        if (countdown <= 0f)
        {
            SpawnEnemy();
            countdown = timeBetweenWaves; // 다시 3초 대기
        }

        countdown -= Time.deltaTime; // 시간 흐르게 하기
    }

    void SpawnEnemy()
    {
        // 내 위치(Spawner 위치)에 적을 생성합니다.
        Instantiate(enemyPrefab, transform.position, transform.rotation);
    }
}
