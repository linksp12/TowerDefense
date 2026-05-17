using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int maxPlayerHp = 20;
    private int currentPlayerHp;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentPlayerHp = maxPlayerHp;
    }

    public void TakePlayerDamage(int damage)
    {
        currentPlayerHp -= damage;
        Debug.Log("플레이어 피격! 남은 라이프: " + currentPlayerHp);

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
