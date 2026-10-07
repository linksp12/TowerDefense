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
    private bool isSceneTransitioning = false;
    private LastOfTheTower.Progression.BattleSession battleSession;
    private LastOfTheTower.Progression.StageProgressService stageProgress;

    private void Awake()
    {
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
        UpdateShieldVisual();

        Canvas gameCanvas = moneyText != null
            ? moneyText.canvas
            : FindAnyObjectByType<Canvas>();
        GameSpeedController.Create(gameCanvas, testTimeScale);
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

    public void TakePlayerDamage(int damage)
    {
        if (isGameEnded) return;

        currentPlayerHp -= damage;

        if (currentPlayerHp < 0)
            currentPlayerHp = 0;

        UpdateHpText();
        UpdateShieldVisual();

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
        if (shieldRenderer == null || maxPlayerHp <= 0)
            return;

        float hpRatio = (float)currentPlayerHp / maxPlayerHp;

        if (hpRatio > 0.6f)
        {
            shieldRenderer.sprite = shieldHealthy;
        }
        else if (hpRatio > 0.2f)
        {
            shieldRenderer.sprite = shieldDamaged;
        }
        else
        {
            shieldRenderer.sprite = shieldCritical;
        }
    }

    private void GameOver()
    {
        BeginResultTransition(false);
    }

    public void GameClear()
    {
        BeginResultTransition(true);
    }

    private void BeginResultTransition(bool victory)
    {
        if (isGameEnded || isSceneTransitioning)
            return;

        isGameEnded = true;
        isSceneTransitioning = true;

        string currentSceneName = SceneManager.GetActiveScene().name;
        string nextSceneName = GetNextStageSceneName(currentSceneName);
        string nextStageId = LastOfTheTower.Progression.StageIds.FromSceneName(nextSceneName);
        bool newlyUnlocked = false;
        bool progressSaveFailed = false;

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
        }

        Debug.Log(victory ? "게임 클리어" : "게임 오버");

        Time.timeScale = 1f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlayStageResultSound();
        }

        PrepareGameUiForResult();

        // 최종 스테이지 승리만 기존 ResultScene으로 이동한다.
        if (victory && currentSceneName == "Stage4Scene")
        {
            ResultSceneManager.isVictory = true;
            ResultSceneManager.restartSceneName = currentSceneName;
            StartCoroutine(LoadResultScene());
            return;
        }

        // Stage1~3 승리와 모든 스테이지 패배는 현재 화면 위에 결과창을 표시한다.
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

        UIManager uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
            uiManager.PrepareForGameResult();
    }

    private string GetNextStageSceneName(string currentSceneName)
    {
        switch (currentSceneName)
        {
            case "Stage1Scene":
                return "Stage2Scene";
            case "Stage2Scene":
                return "Stage3Scene";
            case "Stage3Scene":
                return "Stage4Scene";
            default:
                return string.Empty;
        }
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
