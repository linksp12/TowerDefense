using UnityEngine;

public class MonsterHealth : MonoBehaviour
{
    public int maxHp = 100;
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
            Debug.Log("몬스터 사망");
            Destroy(gameObject);
        }
    }
}
