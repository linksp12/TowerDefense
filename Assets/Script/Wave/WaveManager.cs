using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class WaveManager : MonoBehaviour
{
    [Header("웨이브 설정")]
    public WaveData[] waves;
    public MonsterSpawner spawner;
    public float timeBetweenWaves = 3f;

    [Header("이벤트")]
    public UnityEvent<int> onWaveStart;
    public UnityEvent<int> onWaveCleared;
    public UnityEvent onAllWavesCleared;

    private int currentWaveIndex = 0;
    private int aliveMonsterCount = 0;
    private bool isSpawningDone = false;

    public int CurrentWave => currentWaveIndex + 1;
    public int TotalWaves => waves.Length;

    void Start()
    {
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        for (currentWaveIndex = 0; currentWaveIndex < waves.Length; currentWaveIndex++)
        {
            WaveData wave = waves[currentWaveIndex];

            aliveMonsterCount = 0;
            isSpawningDone = false;

            yield return new WaitForSeconds(wave.waveStartDelay);

            onWaveStart?.Invoke(CurrentWave);
            Debug.Log($"Wave {CurrentWave} / {TotalWaves} 시작!");

            yield return StartCoroutine(spawner.SpawnWave(wave, OnMonsterSpawned));

            isSpawningDone = true;

            Debug.Log($"스폰 완료! 남은 몬스터: {aliveMonsterCount}");

            yield return new WaitUntil(() => isSpawningDone && aliveMonsterCount <= 0);

            onWaveCleared?.Invoke(CurrentWave);
            Debug.Log($"Wave {CurrentWave} 클리어!");

            if (currentWaveIndex < waves.Length - 1)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        onAllWavesCleared?.Invoke();
        Debug.Log("모든 웨이브 클리어!");

        ResultSceneManager.isVictory = true;
        SceneManager.LoadScene("ResultScene");
    }

    void OnMonsterSpawned(GameObject monster)
    {
        aliveMonsterCount++;
        Debug.Log($"몬스터 스폰! 현재 카운트: {aliveMonsterCount}");
    }

    public void OnMonsterKilled()
    {
        aliveMonsterCount--;

        if (aliveMonsterCount < 0)
            aliveMonsterCount = 0;

        Debug.Log($"몬스터 처치! 남은 카운트: {aliveMonsterCount}");
    }

    public void OnMonsterPassed()
    {
        aliveMonsterCount--;

        if (aliveMonsterCount < 0)
            aliveMonsterCount = 0;

        Debug.Log($"몬스터 통과! 남은 카운트: {aliveMonsterCount}");
    }
}