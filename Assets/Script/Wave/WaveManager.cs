using UnityEngine;
using UnityEngine.Events;
using System.Collections;

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

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        for (
            currentWaveIndex = 0;
            currentWaveIndex < waves.Length;
            currentWaveIndex++
        )
        {
            if (IsGameEnded())
                yield break;

            WaveData wave = waves[currentWaveIndex];

            aliveMonsterCount = 0;
            isSpawningDone = false;

            // 웨이브 시작 전 대기
            yield return new WaitForSeconds(wave.waveStartDelay);

            if (IsGameEnded())
                yield break;

            // 웨이브 시작 이벤트
            onWaveStart?.Invoke(CurrentWave);

            Debug.Log(
                $"Wave {CurrentWave} / {TotalWaves} 시작!"
            );

            if (spawner == null)
            {
                Debug.LogError(
                    "WaveManager: MonsterSpawner가 연결되지 않았습니다."
                );

                yield break;
            }

            // 웨이브 몬스터 생성
            yield return StartCoroutine(
                spawner.SpawnWave(
                    wave,
                    OnMonsterSpawned
                )
            );

            isSpawningDone = true;

            Debug.Log(
                $"스폰 완료! 남은 몬스터: {aliveMonsterCount}"
            );

            // 모든 몬스터가 죽거나 도착할 때까지 대기
            yield return new WaitUntil(() =>
                IsGameEnded() ||
                (
                    isSpawningDone &&
                    aliveMonsterCount <= 0
                )
            );

            if (IsGameEnded())
                yield break;

            // 웨이브 클리어
            onWaveCleared?.Invoke(CurrentWave);

            Debug.Log(
                $"Wave {CurrentWave} 클리어!"
            );

            // 다음 웨이브까지 대기
            if (currentWaveIndex < waves.Length - 1)
            {
                yield return new WaitForSeconds(
                    timeBetweenWaves
                );
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

        onAllWavesCleared?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameClear();
        }
        else
        {
            Debug.LogError(
                "WaveManager: GameManager.Instance가 없습니다."
            );
        }
    }

    private void OnMonsterSpawned(GameObject monster)
    {
        if (IsGameEnded())
            return;

        if (monster != null)
        {
            aliveMonsterCount++;
        }
    }

    public void OnMonsterKilled()
    {
        if (IsGameEnded())
            return;

        aliveMonsterCount--;

        if (aliveMonsterCount < 0)
            aliveMonsterCount = 0;
    }

    public void OnMonsterPassed()
    {
        if (IsGameEnded())
            return;

        aliveMonsterCount--;

        if (aliveMonsterCount < 0)
            aliveMonsterCount = 0;
    }

    private bool IsGameEnded()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.IsGameEnded();
    }
}
