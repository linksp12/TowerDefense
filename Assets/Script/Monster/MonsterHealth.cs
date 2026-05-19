using UnityEngine;

public class MonsterHealth : MonoBehaviour
{
    public int maxHp = 100;

    [Header("Reward")]
    public int goldReward = 20;  
    // 처치 시 지급할 돈 — 몬스터마다 다르게 설정 가능

    private int currentHp;

    void Start()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        Debug.Log("몬스터 피격! 남은 HP: " + currentHp);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("몬스터 사망");
 
        // 처치 보상 지급
        if (GameManager.Instance != null)
            GameManager.Instance.AddMoney(goldReward);
 
        Destroy(gameObject);
    }
}

