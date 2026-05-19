using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    public int maxHp = 100;

    [Header("Reward")]
    public int goldReward = 20;

    private int currentHp;

    public Slider monsterHpSlider;

    void Start()
    {
        currentHp = maxHp;

        if (monsterHpSlider != null)
        {
            monsterHpSlider.maxValue = maxHp;
            monsterHpSlider.value = currentHp;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;

        Debug.Log("몬스터 피격! 남은 HP: " + currentHp);

        if (monsterHpSlider != null)
        {
            monsterHpSlider.value = currentHp;
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("몬스터 사망");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(goldReward);
        }

        Destroy(gameObject);
    }
}

