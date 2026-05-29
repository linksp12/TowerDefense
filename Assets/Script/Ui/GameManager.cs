using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Money")]
    public int startingMoney = 500;
    public TextMeshProUGUI moneyText;
    private int currentMoney;

    [Header("Player HP")]
    public int maxPlayerHp = 20;
    private int currentPlayerHp;
    public Slider playerHpSlider;

    [Header("Test Speed")]
    public float testTimeScale = 1f;

    // 게임 종료 중복 실행 방지용
    private bool isGameEnded = false;

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

        if (playerHpSlider != null)
        {
            playerHpSlider.maxValue = maxPlayerHp;
            playerHpSlider.value = currentPlayerHp;
        }
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
            moneyText.text = $"{currentMoney}G";
    }

    public void TakePlayerDamage(int damage)
    {
        if (isGameEnded) return;

        currentPlayerHp -= damage;

        if (currentPlayerHp < 0)
            currentPlayerHp = 0;

        if (playerHpSlider != null)
            playerHpSlider.value = currentPlayerHp;

        if (currentPlayerHp <= 0)
            GameOver();
    }

    private void GameOver()
    {
        if (isGameEnded) return;

        isGameEnded = true;

        Debug.Log("게임 오버 - ResultScene 이동");

        Time.timeScale = 1f;

        ResultSceneManager.isVictory = false;
        SceneManager.LoadScene("ResultScene");
    }

    public void GameClear()
    {
        if (isGameEnded) return;

        isGameEnded = true;

        Debug.Log("게임 클리어 - ResultScene 이동");

        Time.timeScale = 1f;

        ResultSceneManager.isVictory = true;
        SceneManager.LoadScene("ResultScene");
    }

    public bool IsGameEnded()
    {
        return isGameEnded;
    }
}
