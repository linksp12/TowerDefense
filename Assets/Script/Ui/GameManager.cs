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
    public int maxPlayerHp => 5;
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

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = testTimeScale;

        currentMoney = startingMoney;
        UpdateMoneyUI();

        currentPlayerHp = maxPlayerHp;

        UpdateHpText();
        UpdateShieldVisual();
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
                SoundManager.Instance.ShowMoneyWarning();
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
            audioSource.PlayOneShot(damageSound);
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

        Debug.Log(victory ? "게임 클리어" : "게임 오버");

        Time.timeScale = 1f;
        StopGameUiTweens();

        ResultSceneManager.isVictory = victory;
        StartCoroutine(LoadResultScene());
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
