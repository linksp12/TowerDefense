using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    public int maxHp = 100;

    [Header("Reward")]
    public int goldReward = 20;

    private int currentHp;
    private bool isDead = false;

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
        if (isDead) return;

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
        if (isDead) return;
        isDead = true;

        Debug.Log("몬스터 사망");

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnMonsterKilled();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(goldReward);
        }

        Destroy(gameObject);
    }
}
