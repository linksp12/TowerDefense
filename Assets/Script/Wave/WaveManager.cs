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

    // ───────── 다음 웨이브 미리보기·앞당기기 ─────────
    public enum WavePhase { NotStarted, Waiting, Spawning, Fighting, Finished }

    [Header("웨이브 앞당기기")]
    [Tooltip("앞당겨 부를 때 다음 웨이브 처치 골드의 이 비율만큼 바로 준다")]
    [Range(0f, 1f)] public float earlyCallBonusRate = 0.15f;

    public WavePhase Phase { get; private set; } = WavePhase.NotStarted;
    private float waitRemaining;
    private float waitTotal;
    private bool earlyCallRequested;

    public event System.Action<int> EarlyCalled;

    /// <summary>다음에 나올 웨이브(대기 중이면 곧 시작할 웨이브, 싸우는 중이면 그다음 웨이브).</summary>
    public WaveData UpcomingWave
    {
        get
        {
            int index = UpcomingWaveIndex;
            return index >= 0 && index < waves.Length ? waves[index] : null;
        }
    }

    public int UpcomingWaveIndex => Phase == WavePhase.Waiting ? currentWaveIndex
        : Phase == WavePhase.Fighting || Phase == WavePhase.Spawning ? currentWaveIndex + 1 : -1;

    public float WaitRemaining => Phase == WavePhase.Waiting ? Mathf.Max(0f, waitRemaining) : -1f;

    public bool CanCallEarly => wavesStarted && !IsGameEnded() && !earlyCallRequested &&
        (Phase == WavePhase.Waiting ||
         (Phase == WavePhase.Fighting && currentWaveIndex < waves.Length - 1));

    // 싸우는 중에 부르면 전액, 대기 중에 부르면 남은 대기 시간 비율만큼(5골드 단위).
    public int EarlyCallBonus
    {
        get
        {
            WaveData wave = UpcomingWave;
            if (wave == null || wave.spawnInfos == null)
                return 0;
            int gold = 0;
            foreach (WaveData.SpawnInfo info in wave.spawnInfos)
            {
                if (info == null || info.monsterPrefab == null) continue;
                MonsterHealth health = info.monsterPrefab.GetComponent<MonsterHealth>();
                if (health != null && health.monsterData != null)
                    gold += health.monsterData.goldReward * info.count;
            }
            float ratio = Phase == WavePhase.Waiting && waitTotal > 0f ? Mathf.Clamp01(waitRemaining / waitTotal) : 1f;
            int bonus = Mathf.RoundToInt(gold * earlyCallBonusRate * ratio / 5f) * 5;
            return Mathf.Max(5, bonus);
        }
    }

    /// <summary>다음 웨이브를 바로 부르고 보너스 골드를 준다.</summary>
    public bool CallNextWaveEarly()
    {
        if (!CanCallEarly)
            return false;
        int bonus = EarlyCallBonus;
        earlyCallRequested = true;
        if (GameManager.Instance != null && bonus > 0)
            GameManager.Instance.AddMoney(bonus);
        EarlyCalled?.Invoke(bonus);
        return true;
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

            // 남은 몬스터 수는 웨이브를 겹쳐 부를 수 있어 초기화하지 않는다.
            isSpawningDone = false;

            // 웨이브 사이 대기(앞당기면 바로 시작). 배속·일시정지를 따른다.
            Phase = WavePhase.Waiting;
            waitTotal = wave.waveStartDelay + (currentWaveIndex > 0 ? timeBetweenWaves : 0f);
            waitRemaining = waitTotal;
            while (waitRemaining > 0f && !earlyCallRequested)
            {
                if (IsGameEnded())
                    yield break;
                waitRemaining -= Time.deltaTime;
                yield return null;
            }
            earlyCallRequested = false;

            if (IsGameEnded())
                yield break;

            Phase = WavePhase.Spawning;
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
            Phase = WavePhase.Fighting;

            Debug.Log(
                $"스폰 완료! 남은 몬스터: {aliveMonsterCount}"
            );

            // 다 처치하거나, 다음 웨이브를 앞당겨 부를 때까지
            yield return new WaitUntil(() =>
                IsGameEnded() ||
                earlyCallRequested ||
                (
                    isSpawningDone &&
                    aliveMonsterCount <= 0
                )
            );

            if (IsGameEnded())
                yield break;

            // 앞당긴 경우 이번 웨이브는 아직 남은 몬스터와 함께 이어서 싸운다.
            if (earlyCallRequested)
                continue;

            onWaveCleared?.Invoke(CurrentWave);

            Debug.Log(
                $"Wave {CurrentWave} 클리어!"
            );
        }

        Phase = WavePhase.Finished;
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
                TrackBossMusic(monsterHealth);

                if (BossWarningUI.Instance != null)
                    BossWarningUI.Instance.ShowBossWarning();

                if (BossInfoUI.Instance != null)
                    BossInfoUI.Instance.RegisterBoss(
                        monsterHealth
                    );
            }
        }
    }

    // ───────── 보스전 곡: 보스가 나오면 바꾸고, 모두 쓰러지면 전투곡으로 ─────────
    private readonly System.Collections.Generic.List<MonsterHealth> livingBosses =
        new System.Collections.Generic.List<MonsterHealth>();
    private Coroutine bossMusicRoutine;

    private void TrackBossMusic(MonsterHealth boss)
    {
        livingBosses.Add(boss);
        if (bossMusicRoutine != null)
            return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBossBGM();
        bossMusicRoutine = StartCoroutine(WatchBosses());
    }

    private IEnumerator WatchBosses()
    {
        while (true)
        {
            livingBosses.RemoveAll(boss => boss == null || boss.IsDead);
            if (livingBosses.Count == 0 || IsGameEnded())
                break;
            yield return new WaitForSeconds(0.25f);
        }
        bossMusicRoutine = null;
        if (!IsGameEnded() && AudioManager.Instance != null)
            AudioManager.Instance.ResumeBattleBGM();
    }

    /// <summary>웨이브 도중 분열·소환으로 새로 생긴 몬스터를 남은 몬스터 수에 더합니다.</summary>
    public void RegisterSpawnedMonster(GameObject monster)
    {
        OnMonsterSpawned(monster);
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