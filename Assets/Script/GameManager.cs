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

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        currentMoney = startingMoney;
        UpdateMoneyUI();

        currentPlayerHp = maxPlayerHp;

        if (playerHpSlider != null)
        {
            playerHpSlider.maxValue = maxPlayerHp;
            playerHpSlider.value = currentPlayerHp;
        }
    }

    // 돈 추가
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateMoneyUI();
        Debug.Log($"돈 획득: +{amount} / 현재 잔액: {currentMoney}");
    }

    // 돈 사용
    public bool SpendMoney(int amount)
    {
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

    // 돈 충분한지 확인
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

    // 플레이어 체력 감소
    public void TakePlayerDamage(int damage)
    {
        currentPlayerHp -= damage;

        if (playerHpSlider != null)
            playerHpSlider.value = currentPlayerHp;

        if (currentPlayerHp <= 0)
            GameOver();
    }

    private void GameOver()
    {
        Time.timeScale = 0f;
        SceneManager.LoadScene("ResultScene");
    }
}
