using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    public int maxHp = 100;
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

        if (monsterHpSlider != null)
        {
            monsterHpSlider.value = currentHp;
        }

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }
}
