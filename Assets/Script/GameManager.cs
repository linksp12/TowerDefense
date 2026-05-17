using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int maxPlayerHp = 20;
    private int currentPlayerHp;

    public Slider playerHpSlider;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentPlayerHp = maxPlayerHp;

        if (playerHpSlider != null)
        {
            playerHpSlider.maxValue = maxPlayerHp;
            playerHpSlider.value = currentPlayerHp;
        }
    }

    public void TakePlayerDamage(int damage)
    {
        currentPlayerHp -= damage;
        if (playerHpSlider != null)
        {
            playerHpSlider.value = currentPlayerHp;
        }

        if (currentPlayerHp <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        Debug.Log("게임 오버! 결과 씬으로 이동합니다.");
        SceneManager.LoadScene("DefeatScene"); 
    }

    void Update()
    {
        
    }
}
