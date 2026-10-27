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

    public int CurrentWave
    {
        get
        {
            if (waves == null ||
                currentWaveIndex < 0 ||
                currentWaveIndex >= waves.Length ||
                waves[currentWaveIndex] == null)
            {
                return currentWaveIndex + 1;
            }

            return waves[currentWaveIndex].WaveNumber;
        }
    }

    public int TotalWaves =>
        waves != null ? waves.Length : 0;

    [SerializeField]
    private bool waitForTutorial;

    private bool wavesStarted;

    private void Start()
    {
        if (!waitForTutorial)
            StartWaves();
    }

    public void StartWaves()
    {
        if (wavesStarted)
            return;

        if (waves == null ||
            waves.Length == 0)
        {
            Debug.LogError(
                "WaveManager: WaveData가 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (spawner == null)
        {
            Debug.LogError(
                "WaveManager: MonsterSpawner가 연결되지 않았습니다.",
                this
            );

            return;
        }

        wavesStarted = true;

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

            WaveData wave =
                waves[currentWaveIndex];

            if (wave == null)
            {
                Debug.LogError(
                    $"WaveManager: waves[{currentWaveIndex}]에 WaveData가 없습니다.",
                    this
                );

                yield break;
            }

            aliveMonsterCount = 0;
            isSpawningDone = false;

            yield return new WaitForSeconds(
                wave.waveStartDelay
            );

            if (IsGameEnded())
                yield break;

            onWaveStart?.Invoke(CurrentWave);

            Debug.Log(
                $"Wave {CurrentWave} / {TotalWaves} 시작! ID: {wave.WaveId}"
            );

            yield return StartCoroutine(
                spawner.SpawnWave(
                    wave,
                    OnMonsterSpawned
                )
            );

            /*
             * Spawn 실패 시 웨이브와 게임 클리어를 진행하지 않는다.
             */
            if (spawner.HasSpawnError)
            {
                Debug.LogError(
                    $"WaveManager: Wave {wave.WaveNumber}의 스폰 오류로 웨이브 진행을 중단합니다. ID: {wave.WaveId}",
                    this
                );

                yield break;
            }

            isSpawningDone = true;

            Debug.Log(
                $"스폰 완료! 남은 몬스터: {aliveMonsterCount}"
            );

            yield return new WaitUntil(() =>
                IsGameEnded() ||
                (
                    isSpawningDone &&
                    aliveMonsterCount <= 0
                )
            );

            if (IsGameEnded())
                yield break;

            onWaveCleared?.Invoke(CurrentWave);

            Debug.Log(
                $"Wave {CurrentWave} 클리어!"
            );

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

        Debug.Log(
            "모든 웨이브 클리어!"
        );

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

    private void OnMonsterSpawned(
        GameObject monster)
    {
        if (IsGameEnded())
            return;

        if (monster != null)
        {
            aliveMonsterCount++;

            MonsterHealth monsterHealth =
                monster.GetComponent<MonsterHealth>();

            if (monsterHealth != null &&
                monsterHealth.isBoss)
            {
                if (BossWarningUI.Instance != null)
                    BossWarningUI.Instance.ShowBossWarning();

                if (BossInfoUI.Instance != null)
                    BossInfoUI.Instance.RegisterBoss(
                        monsterHealth
                    );
            }
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

    private void OnValidate()
    {
        if (waves == null ||
            waves.Length == 0)
        {
            Debug.LogError(
                "WaveManager: WaveData가 연결되지 않았습니다.",
                this
            );
        }
        else
        {
            for (int i = 0;
                 i < waves.Length;
                 i++)
            {
                if (waves[i] == null)
                {
                    Debug.LogError(
                        $"WaveManager: waves[{i}]가 비어 있습니다.",
                        this
                    );

                    continue;
                }

                if (waves[i].WaveNumber != i + 1)
                {
                    Debug.LogError(
                        $"WaveManager: {waves[i].name}의 Wave Number가 잘못되었습니다. " +
                        $"예상={i + 1}, 실제={waves[i].WaveNumber}",
                        this
                    );
                }

                for (int j = i + 1;
                     j < waves.Length;
                     j++)
                {
                    if (waves[j] == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(
                        waves[i].WaveId))
                    {
                        continue;
                    }

                    if (waves[i].WaveId ==
                        waves[j].WaveId)
                    {
                        Debug.LogError(
                            $"WaveManager: Wave ID가 중복되었습니다. ID={waves[i].WaveId}",
                            this
                        );
                    }
                }
            }
        }

        if (spawner == null)
        {
            Debug.LogError(
                "WaveManager: MonsterSpawner가 연결되지 않았습니다.",
                this
            );
        }
    }

    private bool IsGameEnded()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.IsGameEnded();
    }
}