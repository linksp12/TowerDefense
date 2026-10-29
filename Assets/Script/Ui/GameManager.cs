using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Money")]
    public int startingMoney = 300;
    public TextMeshProUGUI moneyText;
    private int currentMoney;

    [Header("Player HP")]
    public int maxPlayerHp => MainMenuManager.EasterEggPlayerHpOverride > 0
        ? MainMenuManager.EasterEggPlayerHpOverride
        : 15;

    private int currentPlayerHp;
    public int CurrentPlayerHp => currentPlayerHp;
    public TextMeshProUGUI hpText;
    public Image damageImage;
    public AudioSource audioSource;
    public AudioClip damageSound;

    [Header("Test Speed")]
    public float testTimeScale = 1f;

    [Header("Shield UI")]
    public SpriteRenderer shieldRenderer;
    public Sprite shieldHealthy;
    public Sprite shieldDamaged;
    public Sprite shieldCritical;

    private bool isGameEnded = false;
    private int shieldState;
    private Vector3? pendingHitPosition;
    private bool isSceneTransitioning = false;
    private LastOfTheTower.Progression.BattleSession battleSession;

    // 이번 전투의 퀘스트·도전 과제 판정 기록(설치한 타워, 기지 피해)
    public BattleChallengeStats ChallengeStats { get; } = new BattleChallengeStats();
    // 이번 전투의 플레이 기록(처치 수·타워별 피해량·설치 수·시간). 결과창과 누적 플레이 기록이 읽는다.
    // (시간을 읽으므로 필드 초기화가 아니라 Awake에서 만든다)
    public BattleRecord Record { get; private set; }
    private LastOfTheTower.Progression.StageProgressService stageProgress;

    private void Awake()
    {
        Record = new BattleRecord();
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        string stageId = LastOfTheTower.Progression.StageIds.FromSceneName(gameObject.scene.name);
        if (stageId != null)
        {
            battleSession = new LastOfTheTower.Progression.BattleSession(stageId);
            stageProgress = new LastOfTheTower.Progression.StageProgressService(
                new LastOfTheTower.Progression.PlayerPrefsStageProgressStorage());
            stageProgress.Load();
        }

        Time.timeScale = testTimeScale;

        currentMoney = startingMoney;
        UpdateMoneyUI();

        currentPlayerHp = maxPlayerHp;

        UpdateHpText();
        // 'Shield' 태그 기지마다 보호막·체력바 연출을 붙인다. (4스테이지처럼 기지가 여럿이어도 같이 갱신)
        DefenseBase.AttachAll();
        UpdateShieldVisual();

        Canvas gameCanvas = moneyText != null
            ? moneyText.canvas
            : FindAnyObjectByType<Canvas>();
        GameSpeedController.Create(gameCanvas, testTimeScale);

        // 아직 깨지 않은 보스 스테이지는 시작 전에 짧은 대사를 보여 준다.
        if (stageId != null && !stageProgress.IsCleared(stageId))
            StageDialogue.TryShow(stageId);
    }

    public void AddMoney(int amount)
    {
        if (isGameEnded) return;

        currentMoney += amount;
        UpdateMoneyUI();

        Debug.Log($"돈 획득: +{amount} / 현재 잔액: {currentMoney}");
    }

    public bool SpendMoney(int amount)
    {
        if (isGameEnded) return false;

        if (currentMoney < amount)
        {
            Debug.Log($"돈 부족! 필요: {amount} / 보유: {currentMoney}");
            if (SoundManager.Instance != null)
            {
                amount -= currentMoney;
                SoundManager.Instance.ShowMoneyWarning(amount);
            }
            return false;
        }

        currentMoney -= amount;
        UpdateMoneyUI();

        Debug.Log($"돈 사용: -{amount} / 현재 잔액: {currentMoney}");
        return true;
    }

    // 튜토리얼처럼 연습으로 쓴 골드를 되돌릴 때 사용한다. 설치된 타워는 건드리지 않는다.
    public void RestoreStartingMoney()
    {
        if (isGameEnded) return;

        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

    public bool CanAfford(int amount)
    {
        return currentMoney >= amount;
    }

    public int GetCurrentMoney()
    {
        return currentMoney;
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
            moneyText.text = $" {currentMoney}";
    }

    // 몬스터가 도착한 위치를 함께 받으면 가장 가까운 기지가 맞는 연출을 한다.
    public void TakePlayerDamage(int damage, Vector3 hitPosition)
    {
        pendingHitPosition = hitPosition;
        TakePlayerDamage(damage);
        pendingHitPosition = null;
    }

    public void TakePlayerDamage(int damage)
    {
        if (isGameEnded) return;

        if (damage > 0)
            ChallengeStats.RecordBaseDamage();
        currentPlayerHp -= damage;

        if (currentPlayerHp < 0)
            currentPlayerHp = 0;

        int previousShieldState = shieldState;
        UpdateHpText();
        UpdateShieldVisual();
        DefenseBase.PlayHit(pendingHitPosition, shieldState > previousShieldState);

        if (hpText != null)
        {
            hpText.DOKill();
            hpText.color = Color.red;
            hpText.DOColor(Color.white, 0.8f).SetEase(Ease.OutQuad);
        }

        if (damageImage != null)
        {
            damageImage.DOKill();            
            damageImage.color = new Color(0.7f, 0f, 0f, 0.3f);            
            damageImage.DOFade(0f, 0.8f).SetEase(Ease.OutCubic);
        }
        if (audioSource != null && damageSound != null)
        {
            AudioManager.PlaySFXOn(audioSource, damageSound);
        }

        if (currentPlayerHp <= 0)
            GameOver();
    }
    private void UpdateHpText()
    {
        if (hpText != null)
        {
            hpText.text = $" {currentPlayerHp} / {maxPlayerHp}";
        }
    }

    private void UpdateShieldVisual()
    {
        if (maxPlayerHp <= 0)
            return;

        float hpRatio = (float)currentPlayerHp / maxPlayerHp;

        // 0: 온전, 1: 금 감, 2: 위험
        shieldState = hpRatio > 0.6f ? 0 : hpRatio > 0.2f ? 1 : 2;
        Sprite sprite = shieldState == 0 ? shieldHealthy : shieldState == 1 ? shieldDamaged : shieldCritical;

        if (shieldRenderer != null)
            shieldRenderer.sprite = sprite;
        DefenseBase.UpdateAll(hpRatio, shieldState, sprite);
    }

    private void GameOver()
    {
        BeginResultTransition(false);
    }

    public void GameClear()
    {
        if (isGameEnded || isSceneTransitioning)
            return;

        BeginResultTransition(true);
    }

    private void BeginResultTransition(bool victory)
    {
        if (isGameEnded || isSceneTransitioning)
            return;

        isGameEnded = true;
        isSceneTransitioning = true;
        Record.Finish();

        string currentSceneName = SceneManager.GetActiveScene().name;
        string currentStageId = LastOfTheTower.Progression.StageIds.FromSceneName(currentSceneName);
        string nextStageId = LastOfTheTower.Progression.StageIds.GetNextStage(currentStageId);
        string nextSceneName = LastOfTheTower.Progression.StageIds.ToSceneName(nextStageId);
        bool newlyUnlocked = false;
        bool progressSaveFailed = false;
        bool trueEnding = false;

        if (battleSession != null && battleSession.TryComplete(victory, currentPlayerHp, out var result))
        {
            // 최신 기록을 읽고 승리만 저장한다. 패배/중복 종료는 클리어를 지우지 않는다.
            stageProgress.Load();
            bool wasUnlocked = LastOfTheTower.Progression.StageIds.IsUnlocked(stageProgress, nextStageId);
            var status = stageProgress.RecordResult(result);
            progressSaveFailed = victory &&
                (status == LastOfTheTower.Progression.StageClearRecordStatus.LoadRequired ||
                 status == LastOfTheTower.Progression.StageClearRecordStatus.WriteFailed);
            newlyUnlocked = status == LastOfTheTower.Progression.StageClearRecordStatus.SaveRequested &&
                !wasUnlocked && LastOfTheTower.Progression.StageIds.IsUnlocked(stageProgress, nextStageId);
            if (progressSaveFailed)
                Debug.LogError($"스테이지 클리어 기록 저장 실패: {stageProgress.LastError}", this);
            // 이번 승리로 하드코어 전장을 처음 모두 클리어했으면 진엔딩을 한 번 보여 준다.
            trueEnding = status == LastOfTheTower.Progression.StageClearRecordStatus.SaveRequested &&
                LastOfTheTower.Progression.StageIds.IsHardcore(currentStageId) && TowerSkin.IsUnlocked;

            // 확정된 전투 기록을 이번 여정의 플레이 기록(엔딩 카드)에 한 번만 더한다.
            LastOfTheTower.Progression.PlayRecord.AddBattle(Record);

            // 같은 확정 결과(스테이지·승패·남은 체력)를 퀘스트 판정에 한 번만 전달한다.
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnBattleResult(result, ChallengeStats);
        }

        Debug.Log(victory ? "게임 클리어" : "게임 오버");

        Time.timeScale = 1f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlayStageResultSound();
        }

        PrepareGameUiForResult();

        // 본편 최종 스테이지 승리와 하드코어 완주(진엔딩)는 ResultScene(엔딩)으로 이동한다.
        if (victory && (trueEnding || currentStageId == LastOfTheTower.Progression.StageIds.FinalMainStage))
        {
            StorySceneManager.RequestedSequence = trueEnding ? StorySceneManager.Sequence.TrueEnding : (StorySceneManager.Sequence?)null;
            ResultSceneManager.isVictory = true;
            ResultSceneManager.restartSceneName = currentSceneName;
            StartCoroutine(LoadResultScene());
            return;
        }

        // 그 외 승리(하드코어 포함)와 모든 스테이지 패배는 현재 화면 위에 결과창을 표시한다.
        StageResultUI.Show(victory, currentSceneName, currentPlayerHp, maxPlayerHp,
            newlyUnlocked ? nextSceneName : null, progressSaveFailed);
        Time.timeScale = 0f;
    }

    private void PrepareGameUiForResult()
    {
        StopGameUiTweens();

        if (hpText != null)
            hpText.color = Color.white;

        if (damageImage != null)
        {
            Color damageColor = damageImage.color;
            damageColor.a = 0f;
            damageImage.color = damageColor;
        }

        DamagePopup.HideAll();
        CombatFeedback.ClearAll();

        UIManager uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
            uiManager.PrepareForGameResult();

        PauseManager pauseManager = FindAnyObjectByType<PauseManager>();
        if (pauseManager != null)
            pauseManager.CloseForResult();
    }

    private IEnumerator LoadResultScene()
    {
        // 물리 충돌 및 웨이브 콜백이 끝난 다음 프레임에 씬을 전환한다.
        yield return null;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync("ResultScene");

        if (loadOperation == null)
        {
            isSceneTransitioning = false;
            Debug.LogError("ResultScene을 불러오지 못했습니다.");
            yield break;
        }

        yield return loadOperation;
    }

    private void StopGameUiTweens()
    {
        if (hpText != null)
            hpText.DOKill();

        if (damageImage != null)
            damageImage.DOKill();
    }

    private void OnDestroy()
    {
        StopGameUiTweens();

        if (Instance == this)
            Instance = null;
    }

    public bool IsGameEnded()
    {
        return isGameEnded;
    }
}
