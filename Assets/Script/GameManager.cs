using UnityEngine;
using TMPro;  // TextMeshPro 사용 시. 일반 Text면 UnityEngine.UI로 교체

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Starting Money")]
    public int startingMoney = 500;  // 시작 돈 — Inspector에서 자유롭게 변경

    [Header("UI")]
    public TextMeshProUGUI moneyText;  // Canvas의 돈 표시 텍스트 연결

    private int currentMoney;

    private void Awake()
    {
        // 싱글턴 설정
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

    // 돈 추가 (몬스터 처치 보상 등)
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateMoneyUI();
        Debug.Log($"돈 획득: +{amount} / 현재 잔액: {currentMoney}");
    }

    // 돈 차감 (타워 설치 비용)
    // 성공하면 true, 돈 부족하면 false 반환
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

    // 돈이 충분한지 확인만 (차감 없이)
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
}
