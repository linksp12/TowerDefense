using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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
    private bool isAllWavesFinished = false;

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
            if (IsGameEnded())
                yield break;

            WaveData wave = waves[currentWaveIndex];

            aliveMonsterCount = 0;
            isSpawningDone = false;

            yield return new WaitForSeconds(wave.waveStartDelay);

            if (IsGameEnded())
                yield break;

            onWaveStart?.Invoke(CurrentWave);
            Debug.Log($"Wave {CurrentWave} / {TotalWaves} 시작!");

            if (spawner == null)
            {
                Debug.LogError("WaveManager: MonsterSpawner가 연결되지 않았습니다.");
                yield break;
            }

            yield return StartCoroutine(spawner.SpawnWave(wave, OnMonsterSpawned));

            isSpawningDone = true;

            Debug.Log($"스폰 완료! 남은 몬스터: {aliveMonsterCount}");

            yield return new WaitUntil(() =>
                IsGameEnded() || (isSpawningDone && aliveMonsterCount <= 0)
            );

            if (IsGameEnded())
                yield break;

            onWaveCleared?.Invoke(CurrentWave);
            Debug.Log($"Wave {CurrentWave} 클리어!");

            if (currentWaveIndex < waves.Length - 1)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        TryGameClear();
    }

    private void TryGameClear()
    {
        if (IsGameEnded())
            return;

        if (isAllWavesFinished)
            return;

        isAllWavesFinished = true;

        Debug.Log("모든 웨이브 클리어!");

        Time.timeScale = 1f;

        // GameScene의 이벤트는 씬 전환 전에 모두 실행한다.
        onAllWavesCleared?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameClear();
        }
        else
        {
            Debug.LogError("WaveManager: GameManager.Instance가 없습니다.");
        }
    }

    void OnMonsterSpawned(GameObject monster)
    {
        if (IsGameEnded()) return;

        aliveMonsterCount++;
    }

    public void OnMonsterKilled()
    {
        if (IsGameEnded()) return;

        aliveMonsterCount--;

        if (aliveMonsterCount < 0)
            aliveMonsterCount = 0;
    }

    public void OnMonsterPassed()
    {
        if (IsGameEnded()) return;

        aliveMonsterCount--;

        if (aliveMonsterCount < 0)
            aliveMonsterCount = 0;
    }

    private bool IsGameEnded()
    {
        return GameManager.Instance != null && GameManager.Instance.IsGameEnded();
    }
}
